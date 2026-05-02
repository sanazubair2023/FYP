class ApiConfig {
 
  
  // 1. PHYSICAL DEVICE (PC aur Phone ek hi Wi-Fi par hon)
 // static const String ipAddress = '192.168.0.103'; 

  // 2. ANDROID EMULATOR (Agar emulator use karein toh neeche wali line uncomment karein)
  static const String ipAddress = '10.0.2.2';

  // --- PORT SETTINGS ---
  // Visual Studio local dev port
  static const String port = '5000'; 

 
static const String serverBase = "http://$ipAddress:$port";
  // --- ENDPOINTS ---
  // Swagger ke mutabiq controllers ke path
  static const String apiDashboard = '$serverBase/api/Dashboard';
  static const String apiAuth = '$serverBase/api/Auth';
  static const String apiAccount = '$serverBase/api/AccountCreation';
}