$terminalname = "terminal"
$dest_addr_port = "127.0.0.1:554"
$Env:PATH = "$Env:PATH;C:\Program Files\ffmpeg\ffmpeg-master-latest-win64-lgpl\bin;C:\Program Files\ffmpeg\ffmpeg-master-latest-win32-lgpl\bin;"
do {
    ffmpeg -re -f gdigrab -i desktop -pix_fmt yuv420p -vcodec h264 -maxrate 256k -bufsize 2M -f rtsp -vf 'scale=960:540' -profile:v baseline -preset faster -rtsp_transport tcp -tune zerolatency -g 5 -r 1 "rtsp://$dest_addr_port/screen-$terminalname"
    Start-Sleep 2
}
until ($done)