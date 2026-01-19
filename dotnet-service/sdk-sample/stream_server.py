import ctypes
import asyncio
from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse, HTMLResponse
from fastapi.middleware.cors import CORSMiddleware
import threading
import queue

# Assuming Linux SDK path - adjust as needed
SDK_LIB = './src/Hik.Api/HikvisionSDK/HCNetSDK.dll'  # Replace with actual path

# Load the SDK
try:
    sdk = ctypes.CDLL(SDK_LIB)
except OSError:
    print("SDK library not found. Please ensure the Linux Hikvision SDK is installed.")
    exit(1)

# Define basic types and structures (simplified - match actual SDK headers)
class NET_DVR_DEVICEINFO_V30(ctypes.Structure):
    _fields_ = [
        ('sSerialNumber', ctypes.c_char * 48),
        ('byAlarmInPortNum', ctypes.c_byte),
        ('byAlarmOutPortNum', ctypes.c_byte),
        ('byDiskNum', ctypes.c_byte),
        ('byDVRType', ctypes.c_byte),
        ('byChanNum', ctypes.c_byte),
        ('byStartChan', ctypes.c_byte),
        ('byAudioChanNum', ctypes.c_byte),
        ('byIPChanNum', ctypes.c_byte),
        ('byZeroChanNum', ctypes.c_byte),
        ('byMainProto', ctypes.c_byte),
        ('bySubProto', ctypes.c_byte),
        ('bySupport', ctypes.c_byte),
        ('bySupport1', ctypes.c_byte),
        ('bySupport2', ctypes.c_byte),
        ('wDevType', ctypes.c_uint16),
        ('bySupport3', ctypes.c_byte),
        ('byMultiStreamProto', ctypes.c_byte),
        ('byStartDChan', ctypes.c_byte),
        ('byStartDTalkChan', ctypes.c_byte),
        ('byHighDChanNum', ctypes.c_byte),
        ('bySupport4', ctypes.c_byte),
        ('byLanguageType', ctypes.c_byte),
        ('byVoiceInChanNum', ctypes.c_byte),
        ('byStartVoiceInChanNo', ctypes.c_byte),
        ('bySupport5', ctypes.c_byte),
        ('bySupport6', ctypes.c_byte),
        ('byMirrorChanNum', ctypes.c_byte),
        ('wStartMirrorChanNo', ctypes.c_uint16),
        ('bySupport7', ctypes.c_byte),
        ('byRes2', ctypes.c_byte),
    ]

class NET_DVR_PREVIEWINFO(ctypes.Structure):
    _fields_ = [
        ('lChannel', ctypes.c_int),
        ('dwStreamType', ctypes.c_uint32),
        ('dwLinkMode', ctypes.c_uint32),
        ('hPlayWnd', ctypes.c_void_p),
        ('bBlocked', ctypes.c_bool),
        ('dwDisplayBufNum', ctypes.c_uint32),
        ('byProtoType', ctypes.c_byte),
        ('byPreviewMode', ctypes.c_byte),
    ]

# Define function prototypes
sdk.NET_DVR_Init.argtypes = []
sdk.NET_DVR_Init.restype = ctypes.c_bool

sdk.NET_DVR_Login_V30.argtypes = [ctypes.c_char_p, ctypes.c_int, ctypes.c_char_p, ctypes.c_char_p, ctypes.POINTER(NET_DVR_DEVICEINFO_V30)]
sdk.NET_DVR_Login_V30.restype = ctypes.c_int

sdk.NET_DVR_RealPlay_V40.argtypes = [ctypes.c_int, ctypes.POINTER(NET_DVR_PREVIEWINFO), ctypes.c_void_p, ctypes.c_void_p]
sdk.NET_DVR_RealPlay_V40.restype = ctypes.c_int

sdk.NET_DVR_StopRealPlay.argtypes = [ctypes.c_int]
sdk.NET_DVR_StopRealPlay.restype = ctypes.c_bool

sdk.NET_DVR_Logout.argtypes = [ctypes.c_int]
sdk.NET_DVR_Logout.restype = ctypes.c_bool

sdk.NET_DVR_Cleanup.argtypes = []
sdk.NET_DVR_Cleanup.restype = ctypes.c_bool

# Global variables
user_id = -1
playback_id = -1
stream_queue = queue.Queue()

# Callback function for receiving stream data (placeholder)
def stream_callback(lRealHandle, dwDataType, pBuffer, dwBufSize, dwUser):
    if dwDataType == 1:  # Video data
        data = ctypes.string_at(pBuffer, dwBufSize)
        stream_queue.put(data)
    return True

# FastAPI app
app = FastAPI()

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Allow all origins; restrict in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.on_event("startup")
async def startup_event():
    if not sdk.NET_DVR_Init():
        raise RuntimeError("Failed to initialize SDK")
    print("SDK initialized")

@app.on_event("shutdown")
async def shutdown_event():
    if user_id != -1:
        sdk.NET_DVR_Logout(user_id)
    if playback_id != -1:
        sdk.NET_DVR_StopRealPlay(playback_id)
    sdk.NET_DVR_Cleanup()
    print("SDK cleaned up")

@app.post("/login")
def login():
    global user_id
    ip = b"192.168.0.102"
    port = 8000
    user = b"admin"
    password = b"MQPWUI"
    device_info = NET_DVR_DEVICEINFO_V30()
    user_id = sdk.NET_DVR_Login_V30(ip, port, user, password, ctypes.byref(device_info))
    if user_id == -1:
        raise HTTPException(status_code=400, detail="Login failed")
    return {"user_id": user_id}

@app.post("/start-stream/{channel}")
def start_stream(channel: int):
    global playback_id
    if user_id == -1:
        raise HTTPException(status_code=400, detail="Not logged in")
    preview_info = NET_DVR_PREVIEWINFO(
        lChannel=channel,
        dwStreamType=0,
        dwLinkMode=0,
        hPlayWnd=None,
        bBlocked=True,
        dwDisplayBufNum=1,
        byProtoType=0,
        byPreviewMode=0
    )
    # Note: In real implementation, set up callback properly
    playback_id = sdk.NET_DVR_RealPlay_V40(user_id, ctypes.byref(preview_info), None, None)
    if playback_id == -1:
        raise HTTPException(status_code=400, detail="Failed to start stream")
    return {"playback_id": playback_id}

@app.post("/stop-stream")
def stop_stream():
    global playback_id
    if playback_id != -1:
        sdk.NET_DVR_StopRealPlay(playback_id)
        playback_id = -1
    return {"status": "stopped"}

@app.get("/stream")
def stream_video():
    def generate():
        while True:
            try:
                data = stream_queue.get(timeout=1)
                yield data
            except queue.Empty:
                continue
    return StreamingResponse(generate(), media_type="video/mp4")  # Adjust media type as needed

@app.get("/", response_class=HTMLResponse)
def get_html():
    html_content = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Hikvision Live Stream</title>
</head>
<body>
    <h1>Hikvision Live Stream Player</h1>
    <video id="videoPlayer" controls autoplay style="width: 100%; max-width: 800px;">
        <source src="/stream" type="video/mp4">
        Your browser does not support the video tag.
    </video>
    <br><br>
    <button onclick="login()">Login</button>
    <button onclick="startStream()">Start Stream</button>
    <button onclick="stopStream()">Stop Stream</button>

    <script>
        async function login() {
            const response = await fetch('/login', { method: 'POST' });
            const data = await response.json();
            console.log('Login:', data);
        }

        async function startStream() {
            const response = await fetch('/start-stream/1', { method: 'POST' });
            const data = await response.json();
            console.log('Start Stream:', data);
        }

        async function stopStream() {
            const response = await fetch('/stop-stream', { method: 'POST' });
            const data = await response.json();
            console.log('Stop Stream:', data);
        }
    </script>
</body>
</html>
    """
    return html_content

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)