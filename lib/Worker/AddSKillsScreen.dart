import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class AddSkillsScreen extends StatefulWidget {
  @override
  _AddSkillsScreenState createState() => _AddSkillsScreenState();
}

class _AddSkillsScreenState extends State<AddSkillsScreen> {
  String? primary;
  String? secondary;

  Map<String, int> categoryMap = {
    'Cooking': 1,
    'Driving': 2,
    'Cleaning': 3,
  };

  Map<String, IconData> iconMap = {
    'Cooking': Icons.restaurant_menu,
    'Driving': Icons.drive_eta,
    'Cleaning': Icons.cleaning_services,
  };

  void selectPrimary(String skill) {
    setState(() {
      primary = skill;
      if (secondary == skill) secondary = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        elevation: 1,
        backgroundColor: Colors.white,
        leading: BackButton(color: Colors.black),
        title: Text(
          "Add Skills",
          style: TextStyle(color: Colors.black),
        ),
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(20),
        child: Column(
          children: [
            Text("Select Your Primary Skills",
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            SizedBox(height: 15),

            /// Primary Skill Buttons
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                SkillBox(
                  label: "Cleaning",
                  selected: primary == "Cleaning",
                  icon: Icons.cleaning_services,
                  onTap: () => selectPrimary("Cleaning"),
                ),
                SkillBox(
                  label: "Cooking",
                  selected: primary == "Cooking",
                  icon: Icons.restaurant_menu,
                  onTap: () => selectPrimary("Cooking"),
                ),
                SkillBox(
                  label: "Driving",
                  selected: primary == "Driving",
                  icon: Icons.drive_eta,
                  onTap: () => selectPrimary("Driving"),
                ),
              ],
            ),

            SizedBox(height: 25),

            /// Secondary Section
            Text(
              "Select Your Secondary Skills (Optional)",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 10),

            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                SkillBox(
                  label: "Cleaning",
                  selected: secondary == "Cleaning",
                  disabled: primary == "Cleaning",
                  icon: Icons.cleaning_services,
                  onTap: () => setState(() => secondary = "Cleaning"),
                ),
                SkillBox(
                  label: "Cooking",
                  selected: secondary == "Cooking",
                  disabled: primary == "Cooking",
                  icon: Icons.restaurant_menu,
                  onTap: () => setState(() => secondary = "Cooking"),
                ),
                SkillBox(
                  label: "Driving",
                  selected: secondary == "Driving",
                  disabled: primary == "Driving",
                  icon: Icons.drive_eta,
                  onTap: () => setState(() => secondary = "Driving"),
                ),
              ],
            ),

            SizedBox(height: 20),

            if (primary != null)
              ExpertiseSection(
                title: primary!,
                icon: iconMap[primary]!,
                subCategories: ["Skill A", "Skill B", "Skill C"],
              ),

            if (secondary != null)
              ExpertiseSection(
                title: secondary!,
                icon: iconMap[secondary]!,
                subCategories: ["Skill X", "Skill Y"],
              ),

            SizedBox(height: 25),

            ElevatedButton(
              onPressed: () {
                ScaffoldMessenger.of(context)
                    .showSnackBar(SnackBar(content: Text("Skills Saved")));
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: Color.fromRGBO(33, 150, 243, 1),
                fixedSize: Size(double.infinity, 55),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(25)),
              ),
              child: Text(
                "Save and Continue",
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

//
//  Skill Box Widget
//
class SkillBox extends StatelessWidget {
  final String label;
  final bool selected;
  final bool disabled;
  final IconData icon;
  final VoidCallback onTap;

  SkillBox({
    required this.label,
    required this.selected,
    required this.icon,
    required this.onTap,
    this.disabled = false,
  });

  @override
  Widget build(BuildContext context) {
    return Opacity(
      opacity: disabled ? 0.3 : 1,
      child: GestureDetector(
        onTap: disabled ? null : onTap,
        child: AnimatedContainer(
          duration: Duration(milliseconds: 200),
          width: MediaQuery.of(context).size.width * 0.26,
          height: 100,
          decoration: BoxDecoration(
            color: selected ? Colors.green : Colors.white,
            borderRadius: BorderRadius.circular(15),
            boxShadow: [
              BoxShadow(
                  blurRadius: 5, spreadRadius: 1, color: Colors.black12),
            ],
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              CircleAvatar(
                radius: 25,
                backgroundColor:
                    selected ? Colors.white24 : Color(0xFFF0EAF8),
                child: Icon(icon, color: selected ? Colors.white : Colors.black),
              ),
              SizedBox(height: 5),
              Text(
                label,
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  color: selected ? Colors.white : Colors.black,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

//
// Expertise Section Widget
//
class ExpertiseSection extends StatefulWidget {
  final String title;
  final IconData icon;
  final List<String> subCategories;

  ExpertiseSection({
    required this.title,
    required this.icon,
    required this.subCategories,
  });

  @override
  _ExpertiseSectionState createState() => _ExpertiseSectionState();
}

class _ExpertiseSectionState extends State<ExpertiseSection> {
  String? date;
  TextEditingController workAt = TextEditingController();
  TextEditingController description = TextEditingController();
  bool loading = false;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(18),
      margin: EdgeInsets.only(bottom: 20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(25),
        border: Border.all(color: Colors.grey.shade300),
        boxShadow: [
          BoxShadow(blurRadius: 6, color: Colors.black12),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                backgroundColor: Color(0xFFD7E6FF),
                radius: 22,
                child: Icon(widget.icon, color: Color.fromRGBO(33, 150, 243, 1)),
              ),
              SizedBox(width: 12),
              Text("${widget.title} Expertise",
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ],
          ),

          SizedBox(height: 15),

          Text("Sub Categories",
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),

          Wrap(
            spacing: 8,
            children: widget.subCategories
                .map((e) => Chip(
                      label: Text(e),
                      backgroundColor: Colors.grey.shade200,
                    ))
                .toList(),
          ),

          SizedBox(height: 15),

          Text("Working Since",
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),

          GestureDetector(
            onTap: () async {
              DateTime? picked = await showDatePicker(
                context: context,
                firstDate: DateTime(1990),
                lastDate: DateTime.now(),
                initialDate: DateTime.now(),
              );
              if (picked != null) {
                setState(() {
                  date = DateFormat("yyyy-MM-dd").format(picked);
                });
              }
            },
            child: Container(
              padding: EdgeInsets.symmetric(horizontal: 12, vertical: 14),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.grey.shade300),
              ),
              child: Row(
                children: [
                  Expanded(child: Text(date ?? "Select date")),
                  Icon(Icons.calendar_today, size: 20),
                ],
              ),
            ),
          ),

          SizedBox(height: 10),

          buildInput(Icons.location_on, "Worked At", workAt),
          SizedBox(height: 10),
          buildInput(Icons.description, "Description", description),

          SizedBox(height: 10),

          ElevatedButton(
            onPressed: loading
                ? null
                : () {
                    if (date == null ||
                        workAt.text.isEmpty ||
                        description.text.isEmpty) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text("Please complete all fields")),
                      );
                      return;
                    }

                    setState(() => loading = true);

                    Future.delayed(Duration(seconds: 1), () {
                      setState(() => loading = false);
                      ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text("Experience Submitted")));
                    });
                  },
            style: ElevatedButton.styleFrom(
              backgroundColor: loading ? Colors.grey : Color.fromRGBO(33, 150, 243, 1),
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(15)),
              padding: EdgeInsets.symmetric(vertical: 12, horizontal: 20),
            ),
            child: loading
                ? CircularProgressIndicator(color: Colors.white)
                : Text("Submit Exp."),
          ),
        ],
      ),
    );
  }

  Widget buildInput(IconData icon, String hint, TextEditingController ctrl) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 12),
      decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.grey.shade300)),
      child: Row(
        children: [
          Icon(icon, size: 20, color: Colors.grey),
          SizedBox(width: 10),
          Expanded(
            child: TextField(
              controller: ctrl,
              decoration: InputDecoration(
                  hintText: hint, border: InputBorder.none),
            ),
          )
        ],
      ),
    );
  }
}