import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';


const String SERVER_BASE = "YOUR_SERVER_BASE_URL";

class ActiveRequestScreen extends StatefulWidget {
  const ActiveRequestScreen({super.key});

  @override
  State<ActiveRequestScreen> createState() => _ActiveRequestScreenState();
}

class _ActiveRequestScreenState extends State<ActiveRequestScreen> {
  List<dynamic> requests = [];
  bool loading = true;
  String searchQuery = '';
  String activeTab = 'All'; // 'All', 'Pending', 'Approved'

  @override
  void initState() {
    super.initState();
    fetchRequests();
  }

  // -------------------- API: Fetch Requests --------------------
  Future<void> fetchRequests() async {
    setState(() => loading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      String? clientId = prefs.getString('clientId');
      String? token = prefs.getString('userToken');

      final response = await http.get(
        Uri.parse('$SERVER_BASE/api/Dashboard/GetActiveRequests/$clientId'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          requests = json.decode(response.body);
        });
      }
    } catch (error) {
      print("Failed to load requests: $error");
    } finally {
      setState(() => loading = false);
    }
  }

  // -------------------- API: Update Status (Approve/Reject) --------------------
  Future<void> handleHiringDecision(int interviewId, String decision) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.put(
        Uri.parse('$SERVER_BASE/api/Dashboard/UpdateHiringStatus/$interviewId'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: json.encode({'hiringDecision': decision}),
      );

      if (response.statusCode == 200) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text("Interview $decision!"), backgroundColor: Colors.green));
        fetchRequests();
      }
    } catch (error) {
      print("Update failed: $error");
    }
  }

  // -------------------- API: Delete Request --------------------
  Future<void> handleDelete(int interviewId) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.delete(
        Uri.parse('$SERVER_BASE/api/Dashboard/DeleteInterviewRequest/$interviewId'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          requests.removeWhere((r) => r['interviewId'] == interviewId);
        });
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Request deleted.")));
      }
    } catch (error) {
      print("Delete failed: $error");
    }
  }

  // -------------------- Logic: Filter Requests --------------------
  List<dynamic> get filteredRequests {
    return requests.where((item) {
      bool matchesSearch = item['workerName'].toLowerCase().contains(searchQuery.toLowerCase()) ||
          item['workerSkill'].toLowerCase().contains(searchQuery.toLowerCase());

      if (!matchesSearch) return false;

      if (activeTab == 'Pending') {
        return (item['workerDecision'] == 'Pending' || item['hiringDecision'] == 'Pending');
      }
      if (activeTab == 'Approved') {
        return item['hiringDecision'] == 'Accepted';
      }
      return true;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: _buildAppBar(),
      body: Column(
        children: [
          _buildSearchBar(),
          _buildTabs(),
          Expanded(
            child: loading
                ? const Center(child: CircularProgressIndicator(color: Color(0xFF1E64D3)))
                : _buildList(),
          ),
        ],
      ),
    );
  }

  PreferredSizeWidget _buildAppBar() {
    return AppBar(
      backgroundColor: Colors.white,
      elevation: 0,
      leading: IconButton(
        icon: const Icon(Icons.arrow_back, color: Colors.black),
        onPressed: () => Navigator.pop(context),
      ),
      title: const Text("Interview List", style: TextStyle(color: Colors.black, fontWeight: FontWeight.bold)),
      actions: [
        Padding(
          padding: const EdgeInsets.only(right: 15),
          child: Image.asset('assets/images/logo.png', width: 40, height: 40),
        ),
      ],
    );
  }

  Widget _buildSearchBar() {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
      padding: const EdgeInsets.symmetric(horizontal: 15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(25),
        border: Border.all(color: Colors.grey[300]!),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 5)],
      ),
      child: TextField(
        onChanged: (val) => setState(() => searchQuery = val),
        decoration: const InputDecoration(
          icon: Icon(Icons.search, color: Colors.grey),
          hintText: "Search by name or skills",
          border: InputBorder.none,
        ),
      ),
    );
  }

  Widget _buildTabs() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: ['All', 'Pending', 'Approved'].map((tab) {
        bool isActive = activeTab == tab;
        return GestureDetector(
          onTap: () => setState(() => activeTab = tab),
          child: Container(
            margin: const EdgeInsets.symmetric(horizontal: 5, vertical: 10),
            padding: const EdgeInsets.symmetric(horizontal: 25, vertical: 8),
            decoration: BoxDecoration(
              color: isActive ? const Color(0xFF1E64D3) : Colors.white,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: isActive ? const Color(0xFF1E64D3) : Colors.grey[300]!),
            ),
            child: Text(
              tab,
              style: TextStyle(color: isActive ? Colors.white : Colors.grey[700], fontWeight: FontWeight.bold),
            ),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildList() {
    final list = filteredRequests;
    if (list.isEmpty) {
      return const Center(child: Text("No requests match your criteria.", style: TextStyle(color: Colors.grey)));
    }
    return ListView.builder(
      padding: const EdgeInsets.all(20),
      itemCount: list.length,
      itemBuilder: (context, index) => _buildRequestCard(list[index]),
    );
  }

  Widget _buildRequestCard(dynamic item) {
    String workerDecision = item['workerDecision'];
    String hiringDecision = item['hiringDecision'];

    // Conditional Rendering based on Status
    if (workerDecision == 'Rejected') return _statusCard(item, "Interview Rejected", Colors.red, "Not Available right now", false);
    if (workerDecision == 'Accepted' && hiringDecision == 'Accepted') return _statusCard(item, "Interview Accepted", Colors.green, "Verified", true, isAccepted: true);
    if (workerDecision == 'Accepted' && hiringDecision != 'Accepted') return _statusCard(item, "Awaiting Approbation", Colors.orange, "Pending Approval", true, isAwaiting: true);

    // Default: Inprocess
    return _statusCard(item, "Inprocess", Colors.green, "Worker response pending", false);
  }

  Widget _statusCard(dynamic item, String statusText, Color statusColor, String subtitle, bool showApprove, {bool isAccepted = false, bool isAwaiting = false}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 15),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: Colors.grey[400]!),
      ),
      child: Column(
        children: [
          Align(
            alignment: Alignment.topRight,
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(width: 8, height: 8, decoration: BoxDecoration(color: statusColor, shape: BoxShape.circle)),
                const SizedBox(width: 5),
                Text(statusText, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
          Row(
            children: [
              CircleAvatar(
                radius: 35,
                backgroundImage: NetworkImage(item['workerImage'] != null ? "$SERVER_BASE${item['workerImage']}" : "https://via.placeholder.com/150"),
              ),
              const SizedBox(width: 15),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(item['workerName'], style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                    const SizedBox(height: 4),
                    Text(item['workerSkill'], style: const TextStyle(color: Colors.grey, fontSize: 13)),
                    const SizedBox(height: 4),
                    Text(subtitle, style: TextStyle(color: statusColor, fontSize: 12)),
                  ],
                ),
              ),
              Column(
                children: [
                  if (isAwaiting)
                    ElevatedButton(
                      onPressed: () => handleHiringDecision(item['interviewId'], 'Accepted'),
                      style: ElevatedButton.styleFrom(backgroundColor: Colors.green, shape: StadiumBorder()),
                      child: const Text("Approve", style: TextStyle(color: Colors.white, fontSize: 12)),
                    )
                  else if (isAccepted)
                    ElevatedButton(
                      onPressed: () => handleHiringDecision(item['interviewId'], 'Rejected'),
                      style: ElevatedButton.styleFrom(backgroundColor: Colors.blue[100], shape: StadiumBorder()),
                      child: const Text("Reject", style: TextStyle(color: Colors.red, fontSize: 12)),
                    )
                  else
                    ElevatedButton(
                      onPressed: null,
                      style: ElevatedButton.styleFrom(backgroundColor: Colors.grey[200], shape: StadiumBorder()),
                      child: const Text("Approve", style: TextStyle(color: Colors.white, fontSize: 12)),
                    ),
                  TextButton(
                    onPressed: () => handleDelete(item['interviewId']),
                    child: const Text("Delete", style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
                  ),
                ],
              )
            ],
          )
        ],
      ),
    );
  }
}