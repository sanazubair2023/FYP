import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:intl/intl.dart'; 


const String SERVER_BASE = "YOUR_SERVER_BASE_URL";

class DateAndTime extends StatefulWidget {
  final String workerId;
  final String workerName;

  const DateAndTime({
    super.key,
    required this.workerId,
    required this.workerName,
  });

  @override
  State<DateAndTime> createState() => _InterviewSelectionScreenState();
}

class _InterviewSelectionScreenState extends State<DateAndTime> {
  DateTime selectedDate = DateTime.now();
  TimeOfDay selectedTime = TimeOfDay.now();
  bool isLoading = false;
  String clientAddress = 'Loading address...';

  @override
  void initState() {
    super.initState();
    fetchUserData();
  }

  // -------------------- Logic: Fetch User Address --------------------
  Future<void> fetchUserData() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      clientAddress = prefs.getString('userAddress') ?? 'No address found';
    });
  }

  // -------------------- UI Logic: Date Picker --------------------
  Future<void> _selectDate(BuildContext context) async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: selectedDate,
      firstDate: DateTime.now(),
      lastDate: DateTime(2101),
    );
    if (picked != null && picked != selectedDate) {
      setState(() {
        selectedDate = picked;
      });
    }
  }

  // -------------------- UI Logic: Time Picker --------------------
  Future<void> _selectTime(BuildContext context) async {
    final TimeOfDay? picked = await showTimePicker(
      context: context,
      initialTime: selectedTime,
    );
    if (picked != null && picked != selectedTime) {
      setState(() {
        selectedTime = picked;
      });
    }
  }

  // -------------------- API: Book Interview --------------------
  Future<void> handleConfirmInterview() async {
    setState(() => isLoading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      // Date اور Time کو یکجا کر کے ISO فارمیٹ بنانا
      final finalDateTime = DateTime(
        selectedDate.year,
        selectedDate.month,
        selectedDate.day,
        selectedTime.hour,
        selectedTime.minute,
      );

      final response = await http.post(
        Uri.parse('$SERVER_BASE/api/Dashboard/BookInterview'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: json.encode({
          'WorkerId': widget.workerId,
          'InterviewDate': finalDateTime.toIso8601String(),
          'Address': clientAddress,
          'Status': 'Pending',
        }),
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text("Interview request sent to ${widget.workerName}!")),
        );
        Navigator.pop(context); // واپس ڈیش بورڈ پر جانے کے لیے
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text("Failed to book interview.")),
        );
      }
    } catch (error) {
      print("Booking error: $error");
    } finally {
      setState(() => isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FBFF),
      body: SafeArea(
        child: Column(
          children: [
            _buildHeader(),
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      "When do you need to interview ${widget.workerName}?",
                      style: const TextStyle(fontSize: 16, color: Colors.black54),
                    ),
                    const SizedBox(height: 25),
                    
                    // Date Section
                    _buildPickerSection(
                      heading: "Interview Date",
                      label: "Tap to choose date",
                      value: DateFormat('EEE, MMM d, yyyy').format(selectedDate),
                      icon: Icons.calendar_today,
                      iconColor: const Color(0xFF1E64D3),
                      bgColor: const Color(0xFFE3F2FD),
                      onTap: () => _selectDate(context),
                    ),

                    // Time Section
                    _buildPickerSection(
                      heading: "Interview Time",
                      label: "Tap to choose time",
                      value: selectedTime.format(context),
                      icon: Icons.access_time,
                      iconColor: const Color(0xFF6750A4),
                      bgColor: const Color(0xFFF3EDF7),
                      onTap: () => _selectTime(context),
                    ),

                    // Location Section
                    _buildPickerSection(
                      heading: "Interview Location",
                      label: "Your Address",
                      value: clientAddress,
                      icon: Icons.location_on,
                      iconColor: const Color(0xFFE65100),
                      bgColor: const Color(0xFFFFF3E0),
                      onTap: null, // Address صرف ڈسپلے ہو رہا ہے
                    ),
                    
                    const SizedBox(height: 100),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
      bottomSheet: _buildFooter(),
    );
  }

  Widget _buildHeader() {
    return Padding(
      padding: const EdgeInsets.all(20),
      child: Row(
        children: [
          GestureDetector(
            onTap: () => Navigator.pop(context),
            child: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
                boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.1), blurRadius: 5)],
              ),
              child: const Icon(Icons.arrow_back, color: Color(0xFF555555)),
            ),
          ),
          const SizedBox(width: 15),
          const Text(
            "Select Date & Time",
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _buildPickerSection({
    required String heading,
    required String label,
    required String value,
    required IconData icon,
    required Color iconColor,
    required Color bgColor,
    required VoidCallback? onTap,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(heading, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        const SizedBox(height: 10),
        GestureDetector(
          onTap: onTap,
          child: Container(
            padding: const EdgeInsets.all(15),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(15),
              border: Border.all(color: const Color(0xFFEEEEEE)),
              boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.02), blurRadius: 2)],
            ),
            child: Row(
              children: [
                Container(
                  width: 45,
                  height: 45,
                  decoration: BoxDecoration(color: bgColor, shape: BoxShape.circle),
                  child: Icon(icon, color: iconColor),
                ),
                const SizedBox(width: 15),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey)),
                      Text(value, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                    ],
                  ),
                ),
                if (onTap != null) const Icon(Icons.chevron_right, color: Colors.grey),
              ],
            ),
          ),
        ),
        const SizedBox(height: 25),
      ],
    );
  }

  Widget _buildFooter() {
    return Container(
      padding: const EdgeInsets.all(20),
      color: const Color(0xFFF8FBFF),
      child: SizedBox(
        width: double.infinity,
        height: 55,
        child: ElevatedButton(
          onPressed: isLoading ? null : handleConfirmInterview,
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF1E64D3),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
            elevation: 4,
          ),
          child: isLoading
              ? const CircularProgressIndicator(color: Colors.white)
              : const Text(
                  "Confirm Booking",
                  style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                ),
        ),
      ),
    );
  }
}