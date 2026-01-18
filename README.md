
# Cams
Godown 138.252.14.100 8001-8004


docker run -d \
  --name mediamtx \
  --restart unless-stopped \
  -p 8554:8554 \
  -p 8888:8888 \
  -p 8889:8889 \
  -p 9996:9996 \
  -p 9997:9997 \
  -e TZ=Asia/Dhaka \
  -v /Users/ariful/Developer/Personal_Projects/ezviz-camera/web-app/mediamtx.yml:/mediamtx.yml \
  -v /Users/ariful/Developer/Personal_Projects/ezviz-camera/web-app/public/recordings:/recordings \
  bluenviron/mediamtx:1-ffmpeg


Size: 512x288
Fps: 10 fps 
✅ Summary of sizes
Cameras	Days	Storage per camera	Total
140	    10	    6.48 GB	            907 GB
- So roughly ~1 TB to keep last 10 days at this resolution and FPS.


