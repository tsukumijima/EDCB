#ifndef INCLUDE_THREAD_UTIL_H
#define INCLUDE_THREAD_UTIL_H

#ifdef _WIN32
#include <process.h>
#include <exception>
#include <stdexcept>

class atomic_int_
{
public:
	atomic_int_() {}
	atomic_int_(int val) : m_val(val) {}
	int operator=(int val) { exchange(val); return val; }
	operator int() const { return InterlockedExchangeAdd(&m_val, 0); }
	int exchange(int val) { return InterlockedExchange(&m_val, val); }
	int operator++(int) { return InterlockedIncrement(&m_val); }
	int operator--(int) { return InterlockedDecrement(&m_val); }
private:
	atomic_int_(const atomic_int_&);
	atomic_int_& operator=(const atomic_int_&);
	mutable LONG m_val;
};

class atomic_bool_
{
public:
	atomic_bool_() {}
	atomic_bool_(bool val) : m_val(val) {}
	bool operator=(bool val) { return !!(m_val = val); }
	operator bool() const { return !!m_val; }
	bool exchange(bool val) { return !!m_val.exchange(val); }
private:
	atomic_int_ m_val;
};

class thread_
{
public:
	thread_() : m_h(nullptr) {}
	template<class Arg> thread_(void(*f)(Arg), Arg arg) {
		struct Th {
			static UINT WINAPI func(void* p) {
				void(*thf)(Arg) = static_cast<Th*>(p)->f;
				Arg tharg = static_cast<Th*>(p)->arg;
				static_cast<Th*>(p)->b = false;
				thf(tharg);
				return 0;
			}
			Th(void(*thf)(Arg), Arg tharg) : b(true), f(thf), arg(tharg) {}
			atomic_bool_ b;
			void(*f)(Arg);
			Arg arg;
		} th(f, arg);
		m_h = reinterpret_cast<void*>(_beginthreadex(nullptr, 0, Th::func, &th, 0, nullptr));
		if (!m_h) throw std::runtime_error("");
		while (th.b) Sleep(0);
	}
	~thread_() { if (m_h) std::terminate(); }
	bool joinable() const { return !!m_h; }
	void* native_handle() { return m_h; }
	void join() {
		if (!m_h || WaitForSingleObject(m_h, INFINITE) != WAIT_OBJECT_0) throw std::runtime_error("");
		CloseHandle(m_h);
		m_h = nullptr;
	}
	void detach() {
		if (!m_h) throw std::runtime_error("");
		CloseHandle(m_h);
		m_h = nullptr;
	}
	thread_(thread_&& o) : m_h(o.m_h) { o.m_h = nullptr; }
	thread_& operator=(thread_&& o) { if (m_h) std::terminate(); m_h = o.m_h; o.m_h = nullptr; return *this; }
private:
	thread_(const thread_&);
	thread_& operator=(const thread_&);
	void* m_h;
};

class recursive_mutex_
{
public:
	recursive_mutex_() { InitializeCriticalSection(&m_cs); }
	~recursive_mutex_() { DeleteCriticalSection(&m_cs); }
	void lock() { EnterCriticalSection(&m_cs); }
	void unlock() { LeaveCriticalSection(&m_cs); }
private:
	recursive_mutex_(const recursive_mutex_&);
	recursive_mutex_& operator=(const recursive_mutex_&);
	CRITICAL_SECTION m_cs;
};

#else
#include <thread>
#include <mutex>
#include <atomic>
#include <stdexcept>
#include <poll.h>
#include <sys/eventfd.h>
#include <unistd.h>
typedef std::atomic_int atomic_int_;
typedef std::atomic_bool atomic_bool_;
typedef std::thread thread_;
typedef std::recursive_mutex recursive_mutex_;
#endif

class CBlockLock
{
public:
	CBlockLock(recursive_mutex_* mtx) : m_mtx(mtx) { m_mtx->lock(); }
	~CBlockLock() { m_mtx->unlock(); }
private:
	CBlockLock(const CBlockLock&);
	CBlockLock& operator=(const CBlockLock&);
	recursive_mutex_* m_mtx;
};

class CAutoResetEvent
{
public:
#ifdef _WIN32
	CAutoResetEvent(bool initialState = false) {
		m_h = CreateEvent(nullptr, FALSE, initialState, nullptr);
		if (!m_h) throw std::runtime_error("");
	}
	~CAutoResetEvent() { CloseHandle(m_h); }
	void Set() { SetEvent(m_h); }
	void Reset() { ResetEvent(m_h); }
	HANDLE Handle() { return m_h; }
	bool WaitOne(unsigned int timeout = 0xFFFFFFFF) { return WaitForSingleObject(m_h, timeout) == WAIT_OBJECT_0; }
#else
	CAutoResetEvent(bool initialState = false) {
		m_efd = eventfd(0, EFD_CLOEXEC);
		if (m_efd == -1) throw std::runtime_error("");
	}
	~CAutoResetEvent() { close(m_efd); }
	void Set() { __int64 n = 1; write(m_efd, &n, sizeof(n)); }
	void Reset() { WaitOne(0); }
	int Handle() { return m_efd; }
	bool WaitOne(unsigned int timeout = 0xFFFFFFFF) {
		pollfd pfd;
		pfd.fd = m_efd;
		pfd.events = POLLIN;
		if (poll(&pfd, 1, (int)timeout) > 0 && (pfd.revents & POLLIN)) {
			__int64 n;
			return read(m_efd, &n, sizeof(n)) == sizeof(n);
		}
		return false;
	}
#endif
private:
	CAutoResetEvent(const CAutoResetEvent&);
	CAutoResetEvent& operator=(const CAutoResetEvent&);
#ifdef _WIN32
	HANDLE m_h;
#else
	int m_efd;
#endif
};

#endif
