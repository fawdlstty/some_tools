////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  MutexGuard
// Description: 系统互斥量管理类
// Class URI:   
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Nov 14, 2017
//
////////////////////////////////////////////////////////////////////////////////

#ifndef __MUTEX_GUARD_HPP__
#define __MUTEX_GUARD_HPP__

#pragma once

#include <Windows.h>
#include <tchar.h>

class MutexGuard
{
public:
	MutexGuard (LPCSTR lpName)
	{
		m_hMutex = CreateMutexA (NULL, FALSE, lpName);
		if (m_hMutex)
			WaitForSingleObject (m_hMutex, INFINITE);
	}
	~MutexGuard ()
	{
		if (m_hMutex)
		{
			ReleaseMutex (m_hMutex);
			CloseHandle (m_hMutex);
			m_hMutex = NULL;
		}
	}

private:
	HANDLE m_hMutex = NULL;
};

#endif //__MUTEX_GUARD_HPP__
