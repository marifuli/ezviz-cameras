

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

