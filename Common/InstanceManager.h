﻿#pragma once

#include "ThreadUtil.h"
#include <map>
#include <memory>

template <class T>
class CInstanceManager
{
public:
	static const DWORD INVALID_ID = 0xFFFFFFFF;

	CInstanceManager()
	{
		this->m_nextID = 1;
	}

	DWORD push(std::shared_ptr<T> ptr)
	{
		lock_recursive_mutex lock(this->m_lock);

		DWORD id = this->getNextID();
		this->m_list.insert(std::make_pair(id,ptr));

		return id;
	}

	std::shared_ptr<T> pop(DWORD id)
	{
		lock_recursive_mutex lock(this->m_lock);

		std::shared_ptr<T> ptr = this->find(id);
		if (ptr != NULL) {
			this->m_list.erase(id);
		}

		return ptr;
	}

	void clear()
	{
		lock_recursive_mutex lock(this->m_lock);

		this->m_list.clear();
	}

	bool empty() const
	{
		lock_recursive_mutex lock(this->m_lock);

		return this->m_list.empty();
	}

	std::shared_ptr<T> find(DWORD id) const
	{
		lock_recursive_mutex lock(this->m_lock);

		if (this->m_list.count(id) == 0) {
			return NULL;
		}

		return this->m_list.at(id);
	}

protected:
	std::map<DWORD, std::shared_ptr<T> > m_list;
	DWORD m_nextID;
	mutable recursive_mutex_ m_lock;

	DWORD getNextID()
	{
		lock_recursive_mutex lock(this->m_lock);

		DWORD nextID = INVALID_ID;
		int count = 0;
		do {
			if (this->m_list.count(this->m_nextID) == 0) {
				nextID = this->m_nextID;
			}

			this->m_nextID += 1;
			if ((this->m_nextID == 0)|| (this->m_nextID == INVALID_ID)) {
				this->m_nextID = 1;
			}
			count += 1;

			// 65536 回試してダメだったら断念
			// 1 プロセスから (2^16) 以上のインスタンスを同時に作成することはないはず

		} while ((nextID == INVALID_ID) && (count < (1<<16))); 

		return nextID;
	}
};

