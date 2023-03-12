#include "stdafx.h"
#include "LinuxWindowProcedure.h"
#include <chrono>
#include <csignal>
#include <condition_variable>
#include <csignal>
#include <iostream>
#include <mutex>
#include <queue>
#include <thread>

using namespace std::chrono_literals;

bool g_Terminated = false;

void SignalHandler(int signum)
{
	g_Terminated = true;
}

/**
 * Win32 API のウインドウプロシージャに依存しているコードを極力そのまま動かせるようにするためのユーティリティ
 * Written with ChatGPT
*/

CLinuxWindowProcedure::CLinuxWindowProcedure(LRESULT(*WindowProc)(HWND, UINT, WPARAM, LPARAM))
	: m_WindowProc(WindowProc), m_Run(true) {}

// Windows API の SendMessage 的なもの
LRESULT CLinuxWindowProcedure::SendMessage(UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return m_WindowProc(this, uMsg, wParam, lParam);
}

// Windows API の PostMessage 的なもの
BOOL CLinuxWindowProcedure::PostMessage(UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	m_MessageQueue.push({ uMsg, wParam, lParam });
	return TRUE;
}

// Windows API の SetTimer 的なもの
void CLinuxWindowProcedure::SetTimer(UINT_PTR nIDEvent, UINT uElapse)
{
	std::lock_guard<std::mutex> lock(m_TimersMutex);
	// 新しいエントリがすでに存在するかどうかを確認する
	auto it = m_Timers.find(nIDEvent);
	if (it == m_Timers.end()) {
		it = m_Timers.insert({ nIDEvent, std::chrono::system_clock::now() }).first;
	}
	// タイマーの時間を更新する
	it->second = std::chrono::system_clock::now() + std::chrono::milliseconds(uElapse);
}

// Windows API の KillTimer 的なもの
void CLinuxWindowProcedure::KillTimer(UINT_PTR uIDEvent)
{
	std::lock_guard<std::mutex> lock(m_TimersMutex);
	// 削除されるエントリが存在するかどうかを確認する
	auto it = m_Timers.find(uIDEvent);
	if (it != m_Timers.end()) {
		m_Timers.erase(it);
	}
}

// メッセージループを開始する
void CLinuxWindowProcedure::Run()
{
	// SIGINT / SIGTERM を受け取ったら g_Terminated を true にする
	signal(SIGINT, SignalHandler);
	signal(SIGTERM, SignalHandler);

	// 起動処理を行うために WM_CREATE を送信する
	SendMessage(WM_CREATE, 0, 0);

	while (m_Run) {

		while (!m_MessageQueue.empty()) {
			auto message = m_MessageQueue.front();
			m_MessageQueue.pop();

			SendMessage(message.uMsg, message.wParam, message.lParam);

			// 送られてきたメッセージが WM_CLOSE だった場合はメッセージループを終了する
			if (message.uMsg == WM_CLOSE) {
				Exit();
				break;
			}
		}

		auto now = std::chrono::system_clock::now();
		for (auto it = m_Timers.begin(); it != m_Timers.end();) {
			std::lock_guard<std::mutex> lock(m_TimersMutex);
			if (it->second <= now) {
				SendMessage(WM_TIMER, it->first, 0);
				it->second += std::chrono::milliseconds(it->first);
			}
			if (it->second <= now) {
				// Timer event was not processed in time, skip it
				it->second = now + std::chrono::milliseconds(it->first);
			}
			++it;
		}

		// SIGINT が送られてきた場合は終了処理を行った上でメッセージループを終了する
		if (g_Terminated) {
			Exit();
			break;
		}

		std::this_thread::sleep_for(10ms);
	}
}

// メッセージループを終了する
void CLinuxWindowProcedure::Exit()
{
	// 終了処理を行うために WM_DESTROY を送信する
	SendMessage(WM_DESTROY, 0, 0);
	m_Run = false;
}

// HWND は Linux 環境では void* で定義されているので (WinAdapter.h を参照) 、これを LinuxWindowProcedure* にキャストして使う

// Windows API の SendMessage と同じインターフェイスを持つ Polyfill
LRESULT SendMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return reinterpret_cast<CLinuxWindowProcedure*>(hWnd)->SendMessage(uMsg, wParam, lParam);
}

// Windows API の PostMessage と同じインターフェイスを持つ Polyfill
BOOL PostMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return reinterpret_cast<CLinuxWindowProcedure*>(hWnd)->PostMessage(uMsg, wParam, lParam);
}

// Windows API の SetTimer と同じインターフェイスを持つ Polyfill
// TIMERPROC は EDCB では使われていないので利用されない (呼び出し時も常に nullptr が渡される)
UINT_PTR SetTimer(HWND hWnd, UINT_PTR nIDEvent, UINT uElapse, TIMERPROC lpTimerFunc)
{
	reinterpret_cast<CLinuxWindowProcedure*>(hWnd)->SetTimer(nIDEvent, uElapse);
	return nIDEvent;
}

// Windows API の KillTimer と同じインターフェイスを持つ Polyfill
BOOL KillTimer(HWND hWnd, UINT_PTR uIDEvent)
{
	reinterpret_cast<CLinuxWindowProcedure*>(hWnd)->KillTimer(uIDEvent);
	return TRUE;
}
