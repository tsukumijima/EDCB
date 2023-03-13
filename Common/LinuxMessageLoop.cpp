#include "stdafx.h"
#include "LinuxMessageLoop.h"

using namespace std::chrono_literals;

bool g_signalReceived = false;

void SignalHandler(int signum)
{
	g_signalReceived = true;
}

/**
 * Win32 API のウインドウプロシージャに依存しているコードを極力そのまま動かせるようにするためのユーティリティ
 * Written with ChatGPT
*/

CLinuxMessageLoop::CLinuxMessageLoop(LRESULT(*windowProc)(HWND, UINT, WPARAM, LPARAM))
	: m_windowProc(windowProc), m_run(false) {}

// 初期化時に指定されたウインドウプロシージャ関数を実行する
LRESULT CLinuxMessageLoop::RunWindowProcedure(UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return m_windowProc(this, uMsg, wParam, lParam);
}

// Windows API の SendMessage 的なもの
LRESULT CLinuxMessageLoop::SendMessage(UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return RunWindowProcedure(uMsg, wParam, lParam);
}

// Windows API の PostMessage 的なもの
BOOL CLinuxMessageLoop::PostMessage(UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	m_messageQueue.push({ uMsg, wParam, lParam });
	return TRUE;
}

// Windows API の SetTimer 的なもの
void CLinuxMessageLoop::SetTimer(UINT_PTR nIDEvent, UINT uElapse)
{
	// 新しいエントリがすでに存在するかどうかを確認する
	std::lock_guard<std::mutex> lock(m_timersMutex);
	auto it = m_timers.find(nIDEvent);
	if (it == m_timers.end()) {
		TimerData timerData;
		timerData.lastUpdatedAt = std::chrono::time_point_cast<std::chrono::milliseconds>(std::chrono::system_clock::now());
		timerData.intervalSeconds = std::chrono::milliseconds(uElapse);
		it = m_timers.insert({ nIDEvent, timerData }).first;
	}
}

// Windows API の KillTimer 的なもの
void CLinuxMessageLoop::KillTimer(UINT_PTR uIDEvent)
{
	// スレッド上で非同期に KillTimerAsync を実行する
	std::thread([this, uIDEvent]() {
		KillTimerAsync(uIDEvent);
	}).detach();
}

void CLinuxMessageLoop::KillTimerAsync(UINT_PTR uIDEvent)
{
	// 削除されるエントリが存在するかどうかを確認する
	std::lock_guard<std::mutex> lock(m_timersMutex);
	auto it = m_timers.find(uIDEvent);
	if (it != m_timers.end()) {
		m_timers.erase(it);
	}
}

// メッセージループを開始する
void CLinuxMessageLoop::Run()
{
	// SIGINT / SIGTERM を受け取ったら g_signalReceived を true にする
	// signal(SIGINT, SignalHandler);
	// signal(SIGTERM, SignalHandler);

	// 起動処理を行うために WM_CREATE を送信する
	RunWindowProcedure(WM_CREATE, 0, 0);

	m_run = true;
	while (m_run) {

		while (!m_messageQueue.empty()) {
			auto message = m_messageQueue.front();
			m_messageQueue.pop();

			// 送られてきたメッセージをウインドウプロシージャに渡す
			RunWindowProcedure(message.uMsg, message.wParam, message.lParam);

			// 送られてきたメッセージが WM_CLOSE だった場合はメッセージループを終了する
			if (message.uMsg == WM_CLOSE) {
				Exit();
				break;
			}

			// m_run が false になっていた場合はメッセージループを終了する
			if (!m_run) {
				break;
			}
		}

		for (auto it = m_timers.begin(); it != m_timers.end();) {
			std::lock_guard<std::mutex> lock(m_timersMutex);
			auto now = std::chrono::time_point_cast<std::chrono::milliseconds>(std::chrono::system_clock::now());
			auto timerDispatchedAt = std::chrono::time_point_cast<std::chrono::milliseconds>(it->second.lastUpdatedAt + it->second.intervalSeconds);

			if (timerDispatchedAt <= now) {
				RunWindowProcedure(WM_TIMER, it->first, 0);
				it->second.lastUpdatedAt = std::chrono::time_point_cast<std::chrono::milliseconds>(timerDispatchedAt + it->second.intervalSeconds);
			}
			++it;

			// m_run が false になっていた場合はメッセージループを終了する
			if (!m_run) {
				break;
			}
		}

		// SIGINT が送られてきた場合は終了処理を行った上でメッセージループを終了する
		if (g_signalReceived) {
			Exit();
			break;
		}

		// ビジーにならないように少し待機する
		std::this_thread::sleep_for(std::chrono::milliseconds(1));
	}
}

// メッセージループを終了する
void CLinuxMessageLoop::Exit()
{
	// 終了処理を行うために WM_DESTROY を送信する
	RunWindowProcedure(WM_DESTROY, 0, 0);
	m_run = false;
}

// HWND は Linux 環境では void* で定義されているので (WinAdapter.h を参照) 、これを LinuxMessageLoop* にキャストして使う

// Windows API の SendMessage と同じインターフェイスを持つ Polyfill
LRESULT SendMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return reinterpret_cast<CLinuxMessageLoop*>(hWnd)->SendMessage(uMsg, wParam, lParam);
}

// Windows API の PostMessage と同じインターフェイスを持つ Polyfill
BOOL PostMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return reinterpret_cast<CLinuxMessageLoop*>(hWnd)->PostMessage(uMsg, wParam, lParam);
}

// Windows API の SetTimer と同じインターフェイスを持つ Polyfill
// TIMERPROC は EDCB では使われていないので利用されない (呼び出し時も常に nullptr が渡される)
UINT_PTR SetTimer(HWND hWnd, UINT_PTR nIDEvent, UINT uElapse, TIMERPROC lpTimerFunc)
{
	reinterpret_cast<CLinuxMessageLoop*>(hWnd)->SetTimer(nIDEvent, uElapse);
	return nIDEvent;
}

// Windows API の KillTimer と同じインターフェイスを持つ Polyfill
BOOL KillTimer(HWND hWnd, UINT_PTR uIDEvent)
{
	reinterpret_cast<CLinuxMessageLoop*>(hWnd)->KillTimer(uIDEvent);
	return TRUE;
}
