/*
   FreeRDP: A Remote Desktop Protocol client.

   Copyright (C) Jay Sorg 2009-2011

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
//#include <netdb.h>
#include <winsock.h>
//#include <unistd.h>
#include "wait_obj.h"

#define LOG_LEVEL 1
#define LLOG(_level, _args) \
  do { if (_level < LOG_LEVEL) { printf _args ; } } while (0)
#define LLOGLN(_level, _args) \
  do { if (_level < LOG_LEVEL) { printf _args ; printf("\n"); } } while (0)

struct wait_obj
{
	#ifdef _WIN32
		HANDLE event;
	#else
		int pipe_fd[2];
	#endif
};

struct wait_obj *
wait_obj_new(const char * name)
{
	struct wait_obj * obj;

	obj = (struct wait_obj *) malloc(sizeof(struct wait_obj));

	#ifdef _WIN32
		obj->event = CreateEvent(NULL, TRUE, FALSE, NULL);
	#else
		obj->pipe_fd[0] = -1;
		obj->pipe_fd[1] = -1;
		if (pipe(obj->pipe_fd) < 0)
		{
			printf("wait_obj_new: pipe failed\n");
			xfree(obj);
			return NULL;
		}
	#endif

	return obj;
}

int
wait_obj_free(struct wait_obj * obj)
{
	if (obj)
	{
		#ifdef _WIN32
				if (obj->event)
				{
					CloseHandle(obj->event);
					obj->event = NULL;
				}
		#else
				if (obj->pipe_fd[0] != -1)
				{
					close(obj->pipe_fd[0]);
					obj->pipe_fd[0] = -1;
				}
				if (obj->pipe_fd[1] != -1)
				{
					close(obj->pipe_fd[1]);
					obj->pipe_fd[1] = -1;
				}
		#endif

		free(obj);
	}
	return 0;
}

int
wait_obj_is_set(struct wait_obj * obj)
{
	#ifdef _WIN32
		return (WaitForSingleObject(obj->event, 0) == WAIT_OBJECT_0);
	#else
		fd_set rfds;
		int num_set;
		struct timeval time;

		FD_ZERO(&rfds);
		FD_SET(obj->pipe_fd[0], &rfds);
		memset(&time, 0, sizeof(time));
		num_set = select(obj->pipe_fd[0] + 1, &rfds, 0, 0, &time);
		return (num_set == 1);
	#endif
}

int
wait_obj_set(struct wait_obj * obj)
{
	#ifdef _WIN32
		SetEvent(obj->event);
	#else
		int len;

		if (wait_obj_is_set(obj))
			return 0;
		len = write(obj->pipe_fd[1], "sig", 4);
		if (len != 4)
		{
			printf("wait_obj_set: error\n");
			return 1;
		}
	#endif

	return 0;
}


int
wait_obj_clear(struct wait_obj * obj)
{
	#ifdef _WIN32
		ResetEvent(obj->event);
	#else
		int len;

		while (wait_obj_is_set(obj))
		{
			len = read(obj->pipe_fd[0], &len, 4);
			if (len != 4)
			{
				printf("wait_obj_clear: error\n");
				return 1;
			}
		}
	#endif

	return 0;
}

int
wait_obj_select(struct wait_obj ** listobj, int numobj, int * listr, int numr,
	int timeout)
{
	#ifndef _WIN32
		int max;
		int sock;
		int index;
	#endif
		fd_set fds;
		int status;
		struct timeval time;
		struct timeval* ptime;

		ptime = 0;
		if (timeout >= 0)
		{
			time.tv_sec = timeout / 1000;
			time.tv_usec = (timeout * 1000) % 1000000;
			ptime = &time;
		}

	#ifndef _WIN32
		max = 0;
		FD_ZERO(&fds);
		if (listobj)
		{
			for (index = 0; index < numobj; index++)
			{
				sock = listobj[index]->pipe_fd[0];
				FD_SET(sock, &fds);

				if (sock > max)
					max = sock;
			}
		}
		status = select(max + 1, &fds, 0, 0, ptime);
	#else
		status = select(0, &fds, 0, 0, ptime);
	#endif

	return status;
}

