/*
   FreeRDP: A Remote Desktop Protocol client.
   UI types

   Copyright (c) 2009-2011 Jay Sorg
   Copyright (c) 2010-2011 Vic Lee

   Myrtille: A native HTML4/5 Remote Desktop Protocol client.

   Copyright (c) 2014-2016 Cedric Coste

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

#ifndef __WF_TYPES_H
#define __WF_TYPES_H

#include <windows.h>
#include <freerdp/chanman.h>
#include <freerdp/types_ui.h>

#pragma region Myrtille
#include <GdiPlus.h>
#pragma comment(lib, "gdiplus")
using namespace Gdiplus;

#include "webp/encode.h"
#pragma endregion

#define SET_WFI(_inst, _wfi) (_inst)->param1 = _wfi
#define GET_WFI(_inst) ((wfInfo *) ((_inst)->param1))

struct wf_bitmap
{
	HDC hdc;
	HBITMAP bitmap;
	HBITMAP org_bitmap;
};

struct wf_info
{
	/* RDP stuff */
	rdpSet * settings;
	rdpChanMan * chan_man;

	/* UI settings */
	int fs_toggle;
	int fullscreen;
	int percentscreen;

	/* Windows stuff */
	HWND hwnd;
	rdpInst * inst;
	struct wf_bitmap * backstore; /* paint here - InvalidateRect will cause a WM_PAINT event that will BitBlt to hWnd */
	/* state: */
	struct wf_bitmap * drw; /* the current drawing surface - either backstore or something else */
	RD_PALETTE* palette;
	HCURSOR cursor;
	HBRUSH brush;
	HBRUSH org_brush;
	
	#pragma region Myrtille
	/* remote session pipes */
	HANDLE inputsPipe;
	HANDLE imagesPipe;

	int image_encoding;
	int image_quality;

	/* GDI+ */
	ULONG_PTR gdiplusToken;
	CLSID pngClsid;
	CLSID jpgClsid;
	EncoderParameters encoderParameters;

	/* WebP */
	WebPConfig webpConfig;
	#pragma endregion
};
typedef struct wf_info wfInfo;

#ifdef WITH_DEBUG
#define DEBUG(fmt, ...)	printf("DBG (win) %s (%d): " fmt, __FUNCTION__, __LINE__, ## __VA_ARGS__)
#else
#define DEBUG(fmt, ...) do { } while (0)
#endif

#ifdef WITH_DEBUG_KBD
#define DEBUG_KBD(fmt, ...) printf("DBG (win-KBD) %s (%d): " fmt, __FUNCTION__, __LINE__, ## __VA_ARGS__)
#else
#define DEBUG_KBD(fmt, ...) do { } while (0)
#endif

#pragma region Myrtille
/* remote session rdp commands (issued through inputs pipe) */
enum RDP_COMMAND
{
	RDP_COMMAND_SEND_FULLSCREEN_UPDATE = 0,
	RDP_COMMAND_CLOSE_RDP_CLIENT = 1,
	RDP_COMMAND_SET_IMAGE_ENCODING = 2,
	RDP_COMMAND_SET_IMAGE_QUALITY = 3
};

enum IMAGE_FORMAT
{
	IMAGE_FORMAT_PNG = 0,
	IMAGE_FORMAT_JPEG = 1,
	IMAGE_FORMAT_WEBP = 2,
	IMAGE_FORMAT_CUR = 3
};

enum IMAGE_ENCODING
{
	IMAGE_ENCODING_PNG = 0,
	IMAGE_ENCODING_JPEG = 1,	// default
	IMAGE_ENCODING_PNG_JPEG = 2,
	IMAGE_ENCODING_WEBP = 3
};

/* image quality in %
fact is, it may vary depending on the image format...
to keep things easy, and because there are only 2 quality based (lossy) formats managed by this program (JPEG and WEBP... PNG is lossless), we use the same * base * values for all of them... */
enum IMAGE_QUALITY
{
	IMAGE_QUALITY_LOW = 10,
	IMAGE_QUALITY_MEDIUM = 25,
	IMAGE_QUALITY_HIGH = 50,	// default; may be tweaked dynamically depending on image encoding and client bandwidth
	IMAGE_QUALITY_HIGHER = 75,	// used for fullscreen updates
	IMAGE_QUALITY_HIGHEST = 100
};
#pragma endregion

#endif
