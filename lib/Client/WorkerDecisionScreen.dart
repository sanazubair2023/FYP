import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';

// Assuming these are your config constants
const String SERVER_BASE = 'https://servantmaidonline.com';
const String API_BASE = 'https://your-api-url.com/api'; 

class WorkerDecisionScreen extends StatefulWidget {
  const WorkerDecisionScreen({super.key});

  @override
  State<WorkerDecisionScreen> createState() => _WorkerDecisionScreenState();
}

class _WorkerDecisionScreenState extends State<WorkerDecisionScreen> {
  List<dynamic> decisions = [];
  List<dynamic> filteredDecisions = [];
  bool isLoading = true;
  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    fetchDecisions();
  }

  Future<void> fetchDecisions() async {
    setState(() => isLoading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('userToken');

      final response = await http.get(
        Uri.parse('$API_BASE/GetClientWorkerDecisions'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          decisions = json.decode(response.body);
          filteredDecisions = decisions;
        });
      } else {
        _showSnackBar("Failed to fetch decisions.", isError: true);
      }
    } catch (e) {
      _showSnackBar("Server error.", isError: true);
    } finally {
      setState(() => isLoading = false);
    }
  }

  void _filterSearch(String query) {
    setState(() {
      filteredDecisions = decisions
          .where((d) => d['workerName']
              .toString()
              .toLowerCase()
              .contains(query.toLowerCase()))
          .toList();
    });
  }

  Future<void> handleConfirm(int id) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('userToken');

      final response = await http.put(
        Uri.parse('$API_BASE/ClientConfirmWorkerAcceptance/$id'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        _showSnackBar("Worker acceptance successfully confirmed!");
        fetchDecisions();
      } else {
        _showSnackBar("Failed to confirm acceptance.", isError: true);
      }
    } catch (e) {
      _showSnackBar("Network Error", isError: true);
    }
  }

  Future<void> handleDismiss(int id) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('userToken');

      final response = await http.delete(
        Uri.parse('$API_BASE/ClientDismissWorkerRejection/$id'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          decisions.removeWhere((d) => d['id'] == id);
          _filterSearch(_searchController.text);
        });
        Navigator.pushNamed(context, '/FindServiceScreen');
      }
    } catch (e) {
      _showSnackBar("Network Error", isError: true);
    }
  }

  void _showSnackBar(String message, {bool isError = false}) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: isError ? Colors.red : Colors.green,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Stack(
          children: [
            // Visual Background Decoration
            Positioned(
              top: -40,
              left: -40,
              child: Container(
                width: 180,
                height: 180,
                decoration: BoxDecoration(
                  color: const Color(0xFFE3F2FD),
                  borderRadius: BorderRadius.circular(90),
                ),
              ),
            ),
            Column(
              children: [
                _buildHeader(),
                Expanded(
                  child: isLoading
                      ? const Center(child: CircularProgressIndicator())
                      : filteredDecisions.isEmpty
                          ? const Center(
                              child: Text(
                                "No worker decisions available.",
                                style: TextStyle(
                                    fontStyle: FontStyle.italic, color: Colors.grey),
                              ),
                            )
                          : ListView.builder(
                              padding: const EdgeInsets.symmetric(horizontal: 15),
                              itemCount: filteredDecisions.length,
                              itemBuilder: (context, index) =>
                                  _buildDecisionCard(filteredDecisions[index]),
                            ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Padding(
      padding: const EdgeInsets.all(20.0),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              GestureDetector(
                onTap: () => Navigator.pop(context),
                child: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: const BoxDecoration(
                    color: Color(0xFFF5F5F5),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.arrow_back, size: 20, color: Colors.grey),
                ),
              ),
              const Text(
                "Worker Decision",
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              Image.network(
                'https://servantmaidonline.com/logo.png',
                width: 35,
                height: 35,
                errorBuilder: (context, error, stackTrace) =>
                    const Icon(Icons.business),
              ),
            ],
          ),
          const SizedBox(height: 15),
          Container(
            height: 48,
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(25),
              border: Border.all(color: const Color(0xFFE0E0E0)),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.05),
                  blurRadius: 2,
                  offset: const Offset(0, 2),
                )
              ],
            ),
            child: TextField(
              controller: _searchController,
              onChanged: _filterSearch,
              decoration: const InputDecoration(
                hintText: "Search by Worker name",
                hintStyle: TextStyle(color: Colors.grey),
                prefixIcon: Icon(Icons.search, color: Colors.grey),
                border: InputBorder.none,
                contentPadding: EdgeInsets.symmetric(vertical: 10),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDecisionCard(Map<String, dynamic> item) {
    bool isRejected = item['type'] == 'rejected';
    Color themeColor = isRejected ? Colors.red : Colors.green;
    Color statusBg = isRejected ? Colors.red.shade100 : Colors.green.shade100;

    String imageUrl = item['workerImage'] != null &&
            item['workerImage'].toString().startsWith('/')
        ? '$SERVER_BASE${item['workerImage']}'
        : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';

    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: themeColor),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 4,
            offset: const Offset(0, 2),
          )
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isRejected ? 'Worker Rejected!' : 'Worker Accepted!',
            style: TextStyle(
                fontSize: 22, fontWeight: FontWeight.bold, color: themeColor),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Stack(
                children: [
                  CircleAvatar(
                    radius: 37.5,
                    backgroundColor: const Color(0xFFF0F0F0),
                    backgroundImage: NetworkImage(imageUrl),
                  ),
                  Positioned(
                    bottom: 2,
                    right: 2,
                    child: Container(
                      padding: const EdgeInsets.all(2),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.grey.shade300),
                      ),
                      child: const Icon(Icons.verified_user,
                          size: 12, color: Colors.black),
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 15),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item['workerName'] ?? "",
                      style: const TextStyle(
                          fontSize: 24, fontWeight: FontWeight.bold),
                    ),
                    Container(
                      margin: const EdgeInsets.only(top: 6),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 3),
                      decoration: BoxDecoration(
                        color: statusBg,
                        borderRadius: BorderRadius.circular(15),
                      ),
                      child: Text(
                        item['status'] ?? "",
                        style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: themeColor),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 15),
          _infoRow("Decision Date:", item['date']),
          _infoRow("Job Role:", item['role']),
          _infoRow("Address:", item['address']),
          const SizedBox(height: 8),
          Text(
            item['message'] ?? "",
            style: const TextStyle(fontSize: 14, color: Color(0xFF444), height: 1.4),
          ),
          const SizedBox(height: 20),
          Center(child: _buildActionButton(item)),
        ],
      ),
    );
  }

  Widget _infoRow(String label, String? value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 5),
      child: RichText(
        text: TextSpan(
          style: const TextStyle(fontSize: 15, color: Color(0xFF333)),
          children: [
            TextSpan(
                text: "$label ", style: const TextStyle(fontWeight: FontWeight.bold)),
            TextSpan(text: value ?? ""),
          ],
        ),
      ),
    );
  }

  Widget _buildActionButton(Map<String, dynamic> item) {
  if (item['type'] == 'accepted') {
    return ElevatedButton(
      style: ElevatedButton.styleFrom(
        backgroundColor: const Color(0xFF1E64D3),
        padding: const EdgeInsets.symmetric(horizontal: 50, vertical: 10),
        // Is line ko aise update karein:
        shape: const StadiumBorder(), 
      ),
      onPressed: () => handleConfirm(item['id']),
      child: const Text("Confirm", style: TextStyle(color: Colors.white)),
    );
  } else if (item['type'] == 'rejected') {
    return ElevatedButton(
      style: ElevatedButton.styleFrom(
        backgroundColor: const Color(0xFFB0BEC5),
        padding: const EdgeInsets.symmetric(horizontal: 30, vertical: 10),
        shape: const StadiumBorder(),
      ),
      onPressed: () => handleDismiss(item['id']),
      child: const Text("View Other Worker",
          style: TextStyle(color: Colors.white)),
    );
  } else {
    // Baaki code same rahega...
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 50, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.green.withOpacity(0.8),
        borderRadius: BorderRadius.circular(20),
      ),
      child: const Text("Hired",
          style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
    );
  }
  }}