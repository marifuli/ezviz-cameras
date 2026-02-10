
# Server Login
admin@prosoftbd.com
stex##2025 

# Run Dotnet Web service 
dotnet run --project=HikvisionService
# Deploy 
cd ~/ezviz-cameras/HikvisionService && dotnet build && dotnet publish -c Release -o ~/ezviz-cameras/HikvisionService/publish 

# Cams
Godown 138.252.14.100 8001-8004


Size: 512x288
Fps: 10 fps 
✅ Summary of sizes
Cameras	Days	Storage per camera	Total
140	    10	    6.48 GB	            907 GB
- So roughly ~1 TB to keep last 10 days at this resolution and FPS.


