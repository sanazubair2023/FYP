import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

// Config (Apne actual URL se badal dein)
const String API_DASHBOARD = "https://servantmaidonline.com/api/Dashboard";

class ResignationsListScreen extends StatefulWidget {
  const ResignationsListScreen({super.key});

  @override
  State<ResignationsListScreen> createState() => _ResignationsListScreenState();
}

class _ResignationsListScreenState extends State<ResignationsListScreen> {
  List<dynamic> resignations = [];
  bool isLoading = true;

  @override
  void initState() {
    super.initState();
    fetchResignations();
  }

  // -------------------- API Call --------------------
  Future<void> fetchResignations() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.get(
        Uri.parse('$API_DASHBOARD/GetClientResignations'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          resignations = json.decode(response.body);
          isLoading = false;
        });
      } else {
        debugPrint("Failed to fetch resignations");
      }
    } catch (error) {
      debugPrint("Error fetching resignations: $error");
    } finally {
      setState(() => isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FA),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 2,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Color(0xFF333333)),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          "Worker Resignations",
          style: TextStyle(color: Color(0xFF333333), fontWeight: FontWeight.bold, fontSize: 20),
        ),
        centerTitle: true,
      ),
      body: isLoading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF1E64D3)))
          : RefreshIndicator(
              onRefresh: fetchResignations,
              color: const Color(0xFF1E64D3),
              child: resignations.isEmpty
                  ? _buildEmptyState()
                  : ListView.builder(
                      padding: const EdgeInsets.all(20),
                      itemCount: resignations.length,
                      itemBuilder: (context, index) {
                        final item = resignations[index];
                        return _buildResignationBox(item);
                      },
                    ),
            ),
    );
  }

  // -------------------- UI Components --------------------

  Widget _buildResignationBox(dynamic item) {
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: const Color(0xFFEEEEEE)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Box Header
          Row(
           mainAxisAlignment: MainAxisAlignment.spaceBetween,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item['workerName'] ?? "",
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF1E64D3)),
                  ),
                  Text(
                    item['workerRole'] ?? "",
                    style: const TextStyle(fontSize: 13, color: Color(0xFF666666)),
                  ),
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: const Color(0xFFF0F4FF),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text(
                  item['submittedDate'] ?? "",
                  style: const TextStyle(fontSize: 11, color: Color(0xFF1E64D3), fontWeight: FontWeight.w600),
                ),
              ),
            ],
          ),
          const Divider(height: 25, color: Color(0xFFF0F0F0)),

          // Box Content
          const Text(
            "REASON FOR LEAVING:",
            style: TextStyle(fontSize: 12, color: Color(0xFF888888), letterSpacing: 0.5),
          ),
          const SizedBox(height: 5),
          Text(
            item['reason'] ?? "",
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 14, color: Color(0xFF444444), fontStyle: FontStyle.italic),
          ),
          const SizedBox(height: 15),

          // Box Footer
          const Divider(height: 1, color: Color(0xFFF0F0F0)),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  const Icon(Icons.calendar_month, size: 16, color: Color(0xFFE91E63)),
                  const SizedBox(width: 6),
                  Text(
                    "Last Day: ${item['lastWorkingDate']}",
                    style: const TextStyle(fontSize: 13, color: Color(0xFF333333), fontWeight: FontWeight.w600),
                  ),
                ],
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.pushNamed(
                    context,
                    '/resignation',
                    arguments: {'resignationId': item['resignationId'].toString()},
                  );
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF1E64D3),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
                  padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 8),
                  elevation: 2,
                ),
                child: const Row(
                  children: [
                    Text("View Detail", style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13)),
                    Icon(Icons.chevron_right, size: 18, color: Colors.white),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.file_copy_outlined, size: 60, color: Colors.grey[300]),
          const SizedBox(height: 15),
          const Text(
            "No resignation notices received yet.",
            style: TextStyle(color: Color(0xFF999999), fontSize: 16, fontStyle: FontStyle.italic),
          ),
        ],
      ),
    );
  }
}