import 'package:flutter/material.dart';

import 'EditWorkerProfile.dart'; 
import '../Worker/AddSkillsScreen.dart';

class WorkerDashboard extends StatefulWidget {
  const WorkerDashboard({super.key});

  @override
  State<WorkerDashboard> createState() => _WorkerDashboardScreenState();
}

class _WorkerDashboardScreenState extends State<WorkerDashboard> {
  bool isDutyOn = true;
  bool isLoading = false;

  // Mock Data
  final Map<String, dynamic> worker = {
    "name": "Arslan Ahmed",
    "role": "Professional Cook",
    "age": 28,
    "salary": "45,000",
    "gender": "male",
    "location": "Lahore, Pakistan",
    "picture": 'assets/images/logo3.png', 
    "pendingRequestCount": 3,
    "jobNotificationCount": 5,
    "rating": "4.8",
    "reviewCount": 12,
    "primarySkills": ["Cooking", "Baking", "Grilling"],
    "experiences": [
      {"title": "Head Chef at Pearl Continental", "details": "Managed kitchen for 2 years", "period": "2021-2023"},
      {"title": "Sous Chef at PC Hotel", "details": "Assisted in menu planning", "period": "2019-2021"}
    ]
  };

  // Helper functions for Navigation
  void _goToAddSkills() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) =>  AddSkillsScreen(),
      ),
    );
  }

  // Edit Profile پر کلک کرنے پر اب یہ EditWorkerProfile پر جائے گا
  void _goToEditProfile() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => const EditWorkerProfile(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: isLoading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF1E64D3)))
          : SafeArea(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _buildHeader(),
                    _buildEmploymentAction(),
                    _buildDutyStatus(),
                    _buildNotificationTab(
                      icon: Icons.email_outlined,
                      title: "Interview Requests",
                      subtitle: "Pending: ${worker['pendingRequestCount']}",
                      count: "${worker['pendingRequestCount']}",
                      onTap: () {},
                    ),
                    _buildNotificationTab(
                      icon: Icons.notifications_none,
                      title: "Job Notifications",
                      subtitle: "Confirmations and Rejections",
                      count: "${worker['jobNotificationCount']}",
                      onTap: () {},
                    ),
                    _buildFullWidthButton("Check Resignation Status", Colors.red, () {}),
                    _buildSectionTitle("Profile Details"),
                    _buildDetailRow("Salary Expectation", "PKR ${worker['salary']}"),
                    _buildDetailRow("Gender", worker['gender'].toString().toUpperCase()),
                    _buildDetailRow("City", worker['location']),
                    _buildSectionTitle("Experience History"),
                    ...worker['experiences'].asMap().entries.map((entry) {
                      return _buildExperienceItem(
                        title: entry.value['title'],
                        detail: entry.value['details'],
                        period: entry.value['period'],
                        isActive: entry.key == 0,
                      );
                    }).toList(),
                    _buildSectionTitle("My Specialized Skills"),
                    _buildSkillsSection(),
                    _buildSectionTitle("Recent Client Reviews"),
                    _buildReviewsSection(),
                  ],
                ),
              ),
            ),
    );
  }

  // --- UI Components ---

  Widget _buildHeader() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text("Good Afternoon,", style: TextStyle(color: Colors.grey)),
              Text(worker['name'], style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
              Row(
                children: [
                  Text(worker['role'], style: const TextStyle(fontSize: 24, color: Color(0xFF1E64D3), fontWeight: FontWeight.bold)),
                  const SizedBox(width: 5),
                  Text(" • ${worker['age']} Years", style: const TextStyle(fontSize: 14, color: Colors.grey)),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  _headerButton("Logout", () {}),
                  const SizedBox(width: 10),
                  // Edit Profile Button
                  _headerButton("Edit Profile", _goToEditProfile),
                ],
              )
            ],
          ),
        ),
        CircleAvatar(
          radius: 40,
          backgroundImage: worker['picture'].startsWith('assets') 
              ? AssetImage(worker['picture']) as ImageProvider
              : NetworkImage(worker['picture']),
        )
      ],
    );
  }

  Widget _headerButton(String label, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 8),
        decoration: BoxDecoration(color: const Color(0xFF1E64D3), borderRadius: BorderRadius.circular(20)),
        child: Text(label, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
      ),
    );
  }

  Widget _buildEmploymentAction() {
    return Container(
      margin: const EdgeInsets.symmetric(vertical: 20),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(color: Colors.grey[200], borderRadius: BorderRadius.circular(15)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: const [
              Icon(Icons.report_problem_outlined, color: Colors.red),
              SizedBox(width: 10),
              Text("Employment Actions", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            ],
          ),
          const SizedBox(height: 5),
          const Text("Manage your job status and termination requests", style: TextStyle(color: Colors.grey)),
          const SizedBox(height: 15),
          OutlinedButton.icon(
            onPressed: () {},
            icon: const Icon(Icons.cancel_outlined, color: Colors.red),
            label: const Text("Resign from Job", style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
            style: OutlinedButton.styleFrom(
              minimumSize: const Size(double.infinity, 45),
              side: const BorderSide(color: Colors.red),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(25)),
            ),
          )
        ],
      ),
    );
  }

  Widget _buildDutyStatus() {
    return Container(
      padding: const EdgeInsets.all(15),
      margin: const EdgeInsets.only(bottom: 15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.1), blurRadius: 5)],
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text("Duty Status", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              Text(isDutyOn ? "You are visible to customers" : "You are currently hidden", style: const TextStyle(color: Colors.grey)),
            ],
          ),
          Switch(
            value: isDutyOn,
            onChanged: (val) => setState(() => isDutyOn = val),
            activeColor: Colors.green,
          )
        ],
      ),
    );
  }

  Widget _buildNotificationTab({required IconData icon, required String title, required String subtitle, required String count, required VoidCallback onTap}) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          border: Border.all(color: Colors.grey.shade300),
          borderRadius: BorderRadius.circular(15),
        ),
        child: Row(
          children: [
            Container(padding: const EdgeInsets.all(8), decoration: BoxDecoration(color: Colors.grey[100], borderRadius: BorderRadius.circular(10)), child: Icon(icon, color: Colors.black87)),
            const SizedBox(width: 15),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                Text(subtitle, style: const TextStyle(fontSize: 11, color: Colors.grey)),
              ]),
            ),
            CircleAvatar(radius: 12, backgroundColor: Colors.red, child: Text(count, style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.bold))),
          ],
        ),
      ),
    );
  }

  Widget _buildFullWidthButton(String label, Color color, VoidCallback onTap) {
    return Container(
      width: double.infinity,
      height: 45,
      margin: const EdgeInsets.symmetric(vertical: 10),
      child: ElevatedButton(
        onPressed: onTap,
        style: ElevatedButton.styleFrom(backgroundColor: color, shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(25))),
        child: Text(label, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.only(top: 20, bottom: 10),
      child: Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 12),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Color(0xFFEEEEEE)))),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  Widget _buildExperienceItem({required String title, required String detail, required String period, bool isActive = false}) {
    return IntrinsicHeight(
      child: Row(
        children: [
          Column(
            children: [
              Container(width: 12, height: 12, decoration: BoxDecoration(shape: BoxShape.circle, color: isActive ? Colors.blue : Colors.grey[300])),
              Expanded(child: Container(width: 2, color: Colors.grey[200])),
            ],
          ),
          const SizedBox(width: 15),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                  Text("• $detail", style: const TextStyle(fontSize: 13, color: Colors.grey, fontStyle: FontStyle.italic)),
                  Align(alignment: Alignment.centerRight, child: Text(period, style: const TextStyle(fontSize: 11, color: Colors.grey))),
                ],
              ),
            ),
          )
        ],
      ),
    );
  }

  Widget _buildSkillsSection() {
    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: [
        ...(worker['primarySkills'] as List).map((skill) => Container(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
              decoration: BoxDecoration(color: Colors.grey[300], borderRadius: BorderRadius.circular(20)),
              child: Text(skill.toString().toUpperCase(), style: const TextStyle(fontWeight: FontWeight.bold)),
            )),
        OutlinedButton(
          onPressed: _goToAddSkills,
          style: OutlinedButton.styleFrom(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20))),
          child: const Text("+ Edit Skills", style: TextStyle(color: Color(0xFF1E64D3), fontWeight: FontWeight.bold)),
        )
      ],
    );
  }

  Widget _buildReviewsSection() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(color: Colors.grey[100], borderRadius: BorderRadius.circular(20)),
          child: Row(
            children: [
              const Icon(Icons.star, color: Colors.amber, size: 24),
              const SizedBox(width: 8),
              Text(worker['rating'], style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            ],
          ),
        ),
        ElevatedButton(
          onPressed: () {},
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF1E64D3),
            padding: const EdgeInsets.symmetric(horizontal: 30, vertical: 12),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(25)),
          ),
          child: Text("View ${worker['reviewCount']} Reviews", style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        )
      ],
    );
  }
}