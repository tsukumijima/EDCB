#include <chrono>
#include <csignal>
#include <mutex>
#include <queue>
#include <thread>

class CLinuxMessageLoop {
public:
	CLinuxMessageLoop(LRESULT(*windowProc)(HWND, UINT, WPARAM, LPARAM));
	LRESULT RunWindowProcedure(UINT uMsg, WPARAM wParam, LPARAM lParam);
	LRESULT SendMessage(UINT uMsg, WPARAM wParam, LPARAM lParam);
	BOOL PostMessage(UINT uMsg, WPARAM wParam, LPARAM lParam);
	void SetTimer(UINT_PTR nIDEvent, UINT uElapse);
	void KillTimer(UINT_PTR uIDEvent);
	void Run();
	void Exit();
private:
	void KillTimerAsync(UINT_PTR uIDEvent);
	struct Message {
		UINT uMsg;
		WPARAM wParam;
		LPARAM lParam;
	};

	LRESULT(*m_windowProc)(HWND, UINT, WPARAM, LPARAM);
	bool m_run;
	std::queue<Message> m_messageQueue;
	std::map<UINT_PTR, std::chrono::system_clock::time_point> m_timers;
	std::mutex m_timersMutex;
};

LRESULT SendMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
BOOL PostMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
UINT_PTR SetTimer(HWND hWnd, UINT_PTR nIDEvent, UINT uElapse, TIMERPROC lpTimerFunc);
BOOL KillTimer(HWND hWnd, UINT_PTR uIDEvent);
