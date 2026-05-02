import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

// Config placeholders (Apne actual URLs se replace karein)
const String SERVER_BASE = "https://servantmaidonline.com";
const String API_DASHBOARD = "$SERVER_BASE/api/Dashboard";

class ResignationScreen extends StatefulWidget {
  final String resignationId;

  const ResignationScreen({super.key, required this.resignationId});

  @override
  State<ResignationScreen> createState() => _ResignationScreenState();
}

class _ResignationScreenState extends State<ResignationScreen> {
  bool isLoading = true;
  bool isSubmitting = false;
  Map<String, dynamic>? data;
  int rating = 3;
  final TextEditingController _remarksController = TextEditingController();

  @override
  void initState() {
    super.initState();
    fetchResignationDetail();
  }

  // -------------------- API: Fetch Detail --------------------
  Future<void> fetchResignationDetail() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.get(
        Uri.parse('$API_DASHBOARD/GetResignationDetail/${widget.resignationId}'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          data = json.decode(response.body);
          isLoading = false;
        });
      } else {
        Navigator.pop(context);
        _showSnackBar("Failed to load details.", Colors.red);
      }
    } catch (e) {
      _showSnackBar("Network error.", Colors.red);
      setState(() => isLoading = false);
    }
  }

  // -------------------- API: Confirm Resignation --------------------
  Future<void> handleConfirmResignation() async {
    if (_remarksController.text.trim().isEmpty) {
      _showSnackBar("Please enter some remarks before confirming.", Colors.orange);
      return;
    }

    setState(() => isSubmitting = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.post(
        Uri.parse('$API_DASHBOARD/ConfirmResignation'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: json.encode({
          'InterviewId': data?['interviewId'],
          'Rating': rating,
          'Comment': _remarksController.text.trim(),
        }),
      );

      if (response.statusCode == 200) {
        _showSnackBar("Resignation successfully confirmed.", Colors.green);
        Navigator.pushNamedAndRemoveUntil(context, '/clientprofile', (route) => false);
      } else {
        _showSnackBar("Failed to confirm.", Colors.red);
      }
    } catch (e) {
      _showSnackBar("Server error.", Colors.red);
    } finally {
      setState(() => isSubmitting = false);
    }
  }

  void _showSnackBar(String message, Color color) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: color),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator(color: Color(0xFF1E64D3))));
    }

    if (data == null) return const Scaffold(body: Center(child: Text("No Data Found")));

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: Padding(
          padding: const EdgeInsets.all(8.0),
          child: CircleAvatar(
            backgroundColor: const Color(0xFFF0F0F0),
            child: IconButton(
              icon: const Icon(Icons.arrow_back, color: Colors.grey, size: 20),
              onPressed: () => Navigator.pop(context),
            ),
          ),
        ),
        title: const Text("Resignation", style: TextStyle(color: Colors.black, fontWeight: FontWeight.bold, fontSize: 28)),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 15),
            child: Image.network('https://servantmaidonline.com/logo.png', width: 40, height: 40),
          )
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Profile Row
            Row(
              children: [
                CircleAvatar(
                  radius: 40,
                  backgroundColor: Colors.grey[200],
                  backgroundImage: NetworkImage(
                    (data?['workerAvatar'] != null && data!['workerAvatar'].startsWith('/'))
                        ? "$SERVER_BASE${data?['workerAvatar']}"
                        : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png',
                  ),
                ),
                const SizedBox(width: 15),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(data?['workerName'] ?? "", style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
                    Text(data?['workerRole'] ?? "", style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF5B4CF2))),
                  ],
                )
              ],
            ),
            const SizedBox(height: 25),

            // Notice Card
            _buildNoticeCard(),

            // Last Working Day
            _buildSectionTitle("Last Working Day"),
            _buildReadonlyBox(data?['lastWorkingDate'] ?? ""),

            // Reason Section
            _buildSectionTitle("Reason for Leaving"),
            _buildReadonlyBox(data?['reason'] ?? "", minHeight: 100),

            // Remarks & Rating Card
            const SizedBox(height: 10),
            _buildRemarksCard(),

            // Confirm Button
            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              height: 55,
              child: ElevatedButton(
                onPressed: isSubmitting ? null : handleConfirmResignation,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF008000),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                  elevation: 5,
                ),
                child: isSubmitting
                    ? const CircularProgressIndicator(color: Colors.white)
                    : const Text("Confirm Resignation", style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold)),
              ),
            ),
            const SizedBox(height: 40),
          ],
        ),
      ),
    );
  }

  Widget _buildNoticeCard() {
    double progress = (data?['progress'] ?? 0.0).toDouble();
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: Colors.grey[300]!),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(15),
            color: const Color(0xFF6289F4),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text("Official Notice Period", style: TextStyle(color: Colors.white, fontSize: 18)),
                Text("${data?['totalNoticeDays']} Days Total", style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(15),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text("Notice Period Status", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 5),
                RichText(
                  text: TextSpan(
                    style: const TextStyle(color: Colors.grey, fontSize: 16),
                    children: [
                      const TextSpan(text: "Remaining Days: "),
                      TextSpan(text: "${data?['remainingDays']}", style: const TextStyle(color: Color(0xFF5B4CF2), fontWeight: FontWeight.bold)),
                    ],
                  ),
                ),
                const SizedBox(height: 15),
                LinearProgressIndicator(
                  value: progress,
                  minHeight: 6,
                  backgroundColor: Colors.grey[200],
                  valueColor: const AlwaysStoppedAnimation<Color>(Color(0xFF6289F4)),
                ),
              ],
            ),
          )
        ],
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10, top: 10),
      child: Text(title, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
    );
  }

  Widget _buildReadonlyBox(String text, {double? minHeight}) {
    return Container(
      width: double.infinity,
      constraints: BoxConstraints(minHeight: minHeight ?? 0),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: Colors.grey[350]!),
      ),
      child: Text(text, style: const TextStyle(fontSize: 16, color: Colors.grey)),
    );
  }

  Widget _buildRemarksCard() {
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: Colors.grey[350]!),
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: List.generate(5, (index) {
              return IconButton(
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(),
                onPressed: () => setState(() => rating = index + 1),
                icon: Icon(
                  index < rating ? Icons.star : Icons.star_border,
                  color: index < rating ? const Color(0xFFFFD700) : Colors.grey,
                  size: 24,
                ),
              );
            }),
          ),
          const SizedBox(height: 5),
          Container(
            height: 45,
            padding: const EdgeInsets.symmetric(horizontal: 15),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: Colors.grey[350]!),
            ),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _remarksController,
                    decoration: const InputDecoration(
                      hintText: "Enter your remarks here",
                      hintStyle: TextStyle(color: Colors.grey, fontSize: 14),
                      border: InputBorder.none,
                    ),
                  ),
                ),
                ElevatedButton(
                  onPressed: () {}, // Small submit logic if needed
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF1E64D3),
                    minimumSize: const Size(80, 30),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(15)),
                  ),
                  child: const Text("Submit", style: TextStyle(color: Colors.white, fontSize: 13)),
                )
              ],
            ),
          )
        ],
      ),
    );
  }
}