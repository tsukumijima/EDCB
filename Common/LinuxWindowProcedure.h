#include <condition_variable>
#include <queue>

class CLinuxWindowProcedure {
public:
	CLinuxWindowProcedure(LRESULT(*WindowProc)(HWND, UINT, WPARAM, LPARAM));
	LRESULT SendMessage(UINT uMsg, WPARAM wParam, LPARAM lParam);
	BOOL PostMessage(UINT uMsg, WPARAM wParam, LPARAM lParam);
	void SetTimer(UINT_PTR nIDEvent, UINT uElapse);
	void KillTimer(UINT_PTR uIDEvent);
	void Run();
	void Quit();
private:
	struct Message {
		UINT uMsg;
		WPARAM wParam;
		LPARAM lParam;
	};

	LRESULT(*m_WindowProc)(HWND, UINT, WPARAM, LPARAM);
	bool m_Run;
	std::queue<Message> m_MessageQueue;
	std::map<UINT_PTR, std::chrono::system_clock::time_point> m_Timers;
	std::mutex m_TimersMutex;
};

LRESULT SendMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
BOOL PostMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
UINT_PTR SetTimer(HWND hWnd, UINT_PTR nIDEvent, UINT uElapse, TIMERPROC lpTimerFunc);
BOOL KillTimer(HWND hWnd, UINT_PTR uIDEvent);
