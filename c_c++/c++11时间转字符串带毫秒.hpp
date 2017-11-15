////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  Times
// Description: 时间工具类
// Class URI:   
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Sep 22, 2017
//
////////////////////////////////////////////////////////////////////////////////

#ifndef __TIMES_HPP__
#define __TIMES_HPP__

#include <string>
#include <chrono>

class Times
{
	Times () = delete;
public:
	// 获取格式化时间
	static std::string format_time ()
	{
		char buf_time [32], buf_time2 [32];
		buf_time [0] = buf_time2 [0] = '\0';
		auto time_now = std::chrono::system_clock::now ();
		auto duration_in_ms = std::chrono::duration_cast<std::chrono::milliseconds>(time_now.time_since_epoch ());
		auto ms_part = duration_in_ms - std::chrono::duration_cast<std::chrono::seconds>(duration_in_ms);
		time_t raw_time = std::chrono::system_clock::to_time_t (time_now);
		tm local_time_now;
		_localtime64_s (&local_time_now, &raw_time);
		strftime (buf_time2, sizeof (buf_time2), "%Y-%m-%d %H:%M:%S", &local_time_now);
		//char *xx = std::put_time (&local_time_now, "%Y-%m-%d %H:%M:%S");
		_snprintf (buf_time, sizeof(buf_time), "%s.%03d", buf_time2, ms_part.count ());
		return buf_time;
	}
};

#endif //__TIMES_HPP__
