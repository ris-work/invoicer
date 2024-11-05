$terminalname = "terminal"
$dest_addr_port = "127.0.0.1:554"
do {
    ./ffmpeg -re -f gdigrab -i desktop -pix_fmt yuv420p -vcodec h264 -maxrate 256k -bufsize 2M -f rtsp -vf 'scale=iw*0.5:ih*0.5' -profile:v baseline -preset faster -rtsp_transport tcp -tune zerolatency -g 5 -r 1 "rtsp://$dest_addr_port/screen-$terminalname"
    Start-Sleep 2
}
until ($done)