import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

// Maan lijiye aapke config files yahan hain
const String SERVER_BASE = "https://your-api-url.com";

class UserDashboardScreen extends StatefulWidget {
  const UserDashboardScreen({super.key});

  @override
  State<UserDashboardScreen> createState() => _UserDashboardScreenState();
}

class _UserDashboardScreenState extends State<UserDashboardScreen> {
  List<dynamic> workers = [];
  bool isLoading = true;

  // User Info States
  String userName = "Client User";
  String userPicture = "";
  String userAddress = "Your Address";
  String userPhone = "03XXXXXXX";
  String? userEmail;
  String? userId;
  int hiredCount = 0;
  int pendingInterviewsCount = 0;

  @override
  void initState() {
    super.initState();
    loadUserInfo();
    fetchWorkers();
  }

  // 1. Load User Info from SharedPreferences (AsyncStorage replacement)
  Future<void> loadUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      userName = prefs.getString('userName') ?? "Client User";
      userPicture = prefs.getString('userPicture') ?? "";
      userAddress = prefs.getString('userAddress') ?? "Your Address";
      userPhone = prefs.getString('userPhone') ?? "03XXXXXXX";
      userEmail = prefs.getString('userEmail');
      userId = prefs.getString('clientId');
    });
  }

  // 2. Fetch Workers Data from API
  Future<void> fetchWorkers() async {
    setState(() => isLoading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.get(
        Uri.parse('$SERVER_BASE/api/Dashboard/GetClientDashboard'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        setState(() {
          workers = data['hiredWorkers'] ?? [];
          hiredCount = data['hiredCount'] ?? 0;
          pendingInterviewsCount = data['pendingInterviewsCount'] ?? 0;
        });
      } else if (response.statusCode == 401) {
        Navigator.pushReplacementNamed(context, '/login');
      }
    } catch (e) {
      debugPrint("Error fetching dashboard: $e");
    } finally {
      setState(() => isLoading = false);
    }
  }

  Future<void> handleLogout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
    Navigator.pushReplacementNamed(context, '/login');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: isLoading
            ? const Center(child: CircularProgressIndicator(color: Color(0xFF1E64D3)))
            : ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  _buildHeader(),
                  _buildAddressCard(),
                  _buildGridMenu(),
                  _buildSectionTitle("Current Status"),
                  _buildStatusRow(),
                  _buildSectionTitle("Current Worker"),
                  ...workers.map((worker) => _buildWorkerCard(worker)).toList(),
                  if (workers.isEmpty)
                    const Padding(
                      padding: EdgeInsets.only(top: 20),
                      child: Text(
                        "You haven't hired any workers yet.",
                        textAlign: TextAlign.center,
                        style: TextStyle(fontStyle: FontStyle.italic, color: Colors.grey),
                      ),
                    ),
                ],
              ),
      ),
    );
  }

  // --- UI Components ---

  Widget _buildHeader() {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 20),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text("Good Morning", style: TextStyle(fontSize: 13, color: Colors.grey)),
                Text(userName, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
                const SizedBox(height: 15),
                Row(
                  children: [
                    _smallBlueBtn("Logout", handleLogout),
                    const SizedBox(width: 10),
                    _smallBlueBtn("Edit Profile", () {}),
                  ],
                )
              ],
            ),
          ),
          CircleAvatar(
            radius: 45,
            backgroundColor: Colors.grey[200],
            backgroundImage: NetworkImage(
              userPicture.startsWith('/') ? "$SERVER_BASE$userPicture" : 'assets/images/logo1.png',
            ),
          ),
        ],
      ),
    );
  }

  Widget _smallBlueBtn(String label, VoidCallback onPressed) {
    return ElevatedButton(
      onPressed: onPressed,
      style: ElevatedButton.styleFrom(
        backgroundColor: const Color(0xFF1E64D3),
        foregroundColor: Colors.white,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
    );
  }

  Widget _buildAddressCard() {
    return Container(
      margin: const EdgeInsets.only(bottom: 25),
      padding: const EdgeInsets.all(18),
      decoration: _cardDecoration(),
      child: Column(
        children: [
          _infoRow(Icons.location_on, userAddress, Colors.pink),
          const SizedBox(height: 12),
          _infoRow(Icons.phone, userPhone, Colors.black87),
        ],
      ),
    );
  }

  Widget _infoRow(IconData icon, String text, Color iconColor) {
    return Row(
      children: [
        Icon(icon, size: 20, color: iconColor),
        const SizedBox(width: 15),
        Expanded(child: Text(text, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500))),
      ],
    );
  }

  Widget _buildGridMenu() {
    return Wrap(
      spacing: 15,
      runSpacing: 15,
      children: [
        _menuBtn("Services", () => Navigator.pushNamed(context, '/searchworker')),
        _menuBtn("Interview Requests", () => Navigator.pushNamed(context, '/activerequests')),
        _menuBtn("Job Requests", () {}),
        _menuBtn("Resignations", () => Navigator.pushNamed(context, '/resignationslist')),
      ],
    );
  }

  Widget _menuBtn(String title, VoidCallback onTap) {
    return InkWell(
      onTap: onTap,
      child: Container(
        width: MediaQuery.of(context).size.width * 0.42,
        height: 48,
        decoration: BoxDecoration(
          color: const Color(0xFF1E64D3),
          borderRadius: BorderRadius.circular(25),
          boxShadow: [BoxShadow(color: Colors.blue.withOpacity(0.2), blurRadius: 5, offset: const Offset(0, 3))],
        ),
        alignment: Alignment.center,
        child: Text(title, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13)),
      ),
    );
  }

  Widget _buildStatusRow() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        _counterBox(Icons.group, "Workers", hiredCount),
        _counterBox(Icons.history, "Pending", pendingInterviewsCount),
      ],
    );
  }

  Widget _counterBox(IconData icon, String label, int count) {
    return Container(
      width: MediaQuery.of(context).size.width * 0.42,
      padding: const EdgeInsets.all(18),
      decoration: _cardDecoration(),
      child: Row(
        children: [
          Icon(icon, size: 24, color: Colors.black87),
          const SizedBox(width: 10),
          Expanded(child: Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold))),
          Text("$count", style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  Widget _buildWorkerCard(dynamic item) {
    bool isResigned = item['status'] == 'Resigned' || item['type'] == 'alert';

    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.all(18),
      decoration: _cardDecoration(borderColor: isResigned ? Colors.yellow[700] : null),
      child: Column(
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 35,
                backgroundImage: NetworkImage(item['picture'] != null
                    ? "$SERVER_BASE${item['picture']}"
                    : "https://cdn-icons-png.flaticon.com/512/3135/3135715.png"),
              ),
              const SizedBox(width: 18),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(item['name'], style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                      decoration: BoxDecoration(color: Colors.blue[50], borderRadius: BorderRadius.circular(12), border: Border.all(color: const Color(0xFF1E64D3))),
                      child: Text(item['role'] ?? "Worker", style: const TextStyle(fontSize: 10, color: Color(0xFF1E64D3), fontWeight: FontWeight.bold)),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              _statusBadge(item['status'] ?? "On Work"),
              ElevatedButton(
                onPressed: () {},
                style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF1E64D3), shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(15))),
                child: const Text("Terminate", style: TextStyle(color: Colors.white)),
              )
            ],
          )
        ],
      ),
    );
  }

  Widget _statusBadge(String status) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 6),
      decoration: BoxDecoration(color: Colors.blue[50], borderRadius: BorderRadius.circular(12), border: Border.all(color: const Color(0xFF1E64D3))),
      child: Text(status, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFF0056B3))),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 18),
      child: Text(title, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
    );
  }

  BoxDecoration _cardDecoration({Color? borderColor}) {
    return BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(25),
      border: Border.all(color: borderColor ?? const Color(0xFFF0F0F0), width: borderColor != null ? 2 : 1),
      boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10, offset: const Offset(0, 4))],
    );
  }
}