import 'package:flutter/material.dart';

class WorkerDetailScreen extends StatefulWidget {
  final String workerId;

  const WorkerDetailScreen({super.key, required this.workerId});

  @override
  State<WorkerDetailScreen> createState() => _WorkerDetailScreenState();
}

class _WorkerDetailScreenState extends State<WorkerDetailScreen> {
  // Mock Data (Replacing API response)
  final Map<String, dynamic> worker = {
    "id": "1",
    "name": "Jane Doe",
    "role": "Professional Nanny",
    "gender": "Female",
    "age": 28,
    "rating": 4.8,
    "reviewCount": 124,
    "availability": "Available 24/7",
    "location": "New York, NY",
    "salary": "25,000",
    "bio": "Experienced caregiver with over 5 years in child development and early education. Certified in CPR and First Aid.",
    "picture": 'assets/images/logo3.png',
    "primarySkills": ["Child Care", "Education", "Cooking"],
    "partTimeSkills": [
      {
        "categoryName": "Housekeeping",
        "skills": ["Cleaning", "Ironing"]
      }
    ],
    "experiences": [
      {
        "title": "Senior Caretaker",
        "period": "2020 - Present",
        "details": "Managing household schedules and tutoring children."
      },
      {
        "title": "Junior Nanny",
        "period": "2018 - 2020",
        "details": "Assisted in daily routines for toddlers."
      }
    ],
    "reviews": [
      {"reviewerName": "Alice Smith", "rating": 5.0, "comment": "Excellent service!", "date": "2 months ago"},
    ],
    "hasActiveInterview": false,
    "activeInterviewStatus": "None"
  };

  @override
  Widget build(BuildContext context) {
    bool isAvailable = worker['availability'] == "Available 24/7";

    return Scaffold(
      backgroundColor: Colors.white,
      body: Stack(
        children: [
          SingleChildScrollView(
            physics: const BouncingScrollPhysics(),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Profile Image Section
                Stack(
                  children: [
                    Image.network(
                      worker['picture'],
                      width: double.infinity,
                      height: 400,
                      fit: BoxFit.cover,
                    ),
                    Positioned(
                      top: 40,
                      left: 20,
                      child: GestureDetector(
                        onTap: () => Navigator.pop(context),
                        child: Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: Colors.white.withOpacity(0.8),
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.arrow_back, color: Colors.black, size: 24),
                        ),
                      ),
                    ),
                    Positioned(
                      bottom: 15,
                      left: 15,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: Colors.grey.shade200),
                        ),
                        child: Row(
                          children: [
                            Container(
                              width: 8,
                              height: 8,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: isAvailable ? const Color(0xFF4CAF50) : const Color(0xFFFF5722),
                              ),
                            ),
                            const SizedBox(width: 8),
                            Text(
                              isAvailable ? "ACTIVE" : "BOOKED",
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.bold,
                                color: isAvailable ? const Color(0xFF4CAF50) : const Color(0xFFFF5722),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),

                // Content Section
                Padding(
                  padding: const EdgeInsets.all(20.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Name and Rating
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(worker['name'], style: const TextStyle(fontSize: 26, fontWeight: FontWeight.bold)),
                                Row(
                                  children: [
                                    Text(worker['role'], style: const TextStyle(fontSize: 18, color: Color(0xFF1E64D3), fontWeight: FontWeight.bold)),
                                    const SizedBox(width: 15),
                                    Text("${worker['gender'].toUpperCase()} • ${worker['age']} Y/O", 
                                      style: const TextStyle(fontSize: 14, color: Color(0xFF4CAF50), fontWeight: FontWeight.bold)),
                                  ],
                                ),
                              ],
                            ),
                          ),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Row(
                                children: [
                                  const Icon(Icons.star, color: Color(0xFFFFD700), size: 20),
                                  Text("${worker['rating']}", style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
                                ],
                              ),
                              Text("(${worker['reviewCount']} Reviews)", style: const TextStyle(fontSize: 12, color: Colors.grey)),
                            ],
                          )
                        ],
                      ),

                      // Statistics Row
                      Container(
                        margin: const EdgeInsets.symmetric(vertical: 20),
                        padding: const EdgeInsets.all(15),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(15),
                          boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.1), blurRadius: 10)],
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceAround,
                          children: [
                            StatItem(label: "EXPERIENCE", value: worker['experiences'][0]['period']),
                            Container(width: 1, height: 30, color: Colors.grey.shade200),
                            StatItem(label: "LOCATION", value: worker['location'].toUpperCase()),
                            Container(width: 1, height: 30, color: Colors.grey.shade200),
                            StatItem(label: "SALARY", value: "Rs.${worker['salary']}"),
                          ],
                        ),
                      ),

                      const SectionTitle(title: "About"),
                      Text(worker['bio'], style: const TextStyle(fontSize: 14, color: Color(0xFF666666), height: 1.4)),

                      const SectionTitle(title: "Skills"),
                      Wrap(
                        spacing: 10,
                        runSpacing: 10,
                        children: (worker['primarySkills'] as List).map((s) => SkillChip(label: s)).toList(),
                      ),

                      const SectionTitle(title: "Part-Time"),
                      ...(worker['part_time_skills'] ?? []).map<Widget>((item) => Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(item['categoryName'].toUpperCase(), style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: Color(0xFF1E64D3))),
                          const SizedBox(height: 8),
                          Wrap(
                            spacing: 10,
                            runSpacing: 10,
                            children: (item['skills'] as List).map((s) => SkillChip(label: s)).toList(),
                          ),
                          const SizedBox(height: 15),
                        ],
                      )).toList(),

                      const SectionTitle(title: "Work Experience"),
                      ...(worker['experiences'] as List).asMap().entries.map((entry) => ExperienceItem(
                        title: entry.value['title'],
                        period: entry.value['period'],
                        detail: entry.value['details'],
                        isActive: entry.key == 0,
                      )).toList(),

                      const SectionTitle(title: "Booking Procedure"),
                      const ProcedureStep(text: "Send a booking request with your preferred date."),
                      const ProcedureStep(text: "Wait for the worker to accept (usually within 30m)."),
                      const ProcedureStep(text: "Confirm the location and start the service."),

                      const SizedBox(height: 120), // Padding for bottom button
                    ],
                  ),
                ),
              ],
            ),
          ),

          // Sticky Bottom Button
          Positioned(
            bottom: 0,
            left: 0,
            right: 0,
            child: Container(
              padding: const EdgeInsets.all(20),
              color: Colors.white.withOpacity(0.9),
              child: ElevatedButton(
                onPressed: worker['hasActiveInterview'] ? null : () {},
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF1E64D3),
                  disabledBackgroundColor: const Color(0xFFB0BEC5),
                  minimumSize: const Size(double.infinity, 50),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(25)),
                  elevation: 5,
                ),
                child: Text(
                  worker['hasActiveInterview'] ? "Interview Request Pending" : "Call For Interview",
                  style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// Sub-widgets
class StatItem extends StatelessWidget {
  final String label, value;
  const StatItem({super.key, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(label, style: const TextStyle(fontSize: 10, color: Colors.grey)),
        const SizedBox(height: 5),
        Text(value, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold)),
      ],
    );
  }
}

class SectionTitle extends StatelessWidget {
  final String title;
  const SectionTitle({super.key, required this.title});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 15, bottom: 10),
      child: Text(title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
    );
  }
}

class SkillChip extends StatelessWidget {
  final String label;
  const SkillChip({super.key, required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 8),
      decoration: BoxDecoration(color: const Color(0xFFE0E0E0), borderRadius: BorderRadius.circular(20)),
      child: Text(label.toUpperCase(), style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w900, color: Color(0xFF333333))),
    );
  }
}

class ExperienceItem extends StatelessWidget {
  final String title, period, detail;
  final bool isActive;
  const ExperienceItem({super.key, required this.title, required this.period, required this.detail, required this.isActive});

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Container(
              width: 12, height: 12,
              decoration: BoxDecoration(shape: BoxShape.circle, color: isActive ? const Color(0xFF1E64D3) : Colors.grey.shade300),
            ),
            Container(width: 2, height: 50, color: Colors.grey.shade200),
          ],
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                  Text(period, style: const TextStyle(fontSize: 10, color: Colors.grey)),
                ],
              ),
              Text("• $detail", style: const TextStyle(fontSize: 12, color: Colors.grey, fontStyle: FontStyle.italic)),
              const SizedBox(height: 20),
            ],
          ),
        )
      ],
    );
  }
}

class ProcedureStep extends StatelessWidget {
  final String text;
  const ProcedureStep({super.key, required this.text});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text("• $text", style: const TextStyle(fontSize: 13, color: Color(0xFF666666))),
    );
  }
}