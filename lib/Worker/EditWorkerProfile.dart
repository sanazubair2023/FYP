import 'dart:io'; // فائل کے لیے ضروری
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'AddSkillsScreen.dart'; 
void main() {
  runApp(const MaterialApp(
    debugShowCheckedModeBanner: false,
    home: EditWorkerProfile(),
  ));
}

class EditWorkerProfile extends StatefulWidget {
  const EditWorkerProfile({super.key});

  @override
  State<EditWorkerProfile> createState() => _EditWorkerProfileState();
}

class _EditWorkerProfileState extends State<EditWorkerProfile> {
  int step = 1;
  File? _image; // منتخب شدہ تصویر محفوظ کرنے کے لیے
  final ImagePicker _picker = ImagePicker();

  // Controllers
  final name = TextEditingController();
  final age = TextEditingController();
  final phone = TextEditingController();
  final cnic = TextEditingController();
  final salary = TextEditingController();
  final email = TextEditingController();
  final address = TextEditingController();
  final password = TextEditingController();
  final confirmPassword = TextEditingController();

  String gender = "Male";

  // تصویر منتخب کرنے کا فنکشن
  Future<void> _pickImage() async {
    final XFile? pickedFile = await _picker.pickImage(source: ImageSource.gallery);
    if (pickedFile != null) {
      setState(() {
        _image = File(pickedFile.path);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xfff2f2f2),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: step == 1 ? stepOne() : stepTwo(),
        ),
      ),
    );
  }

  // ================= STEP 1 =================
  Widget stepOne() {
    return Column(
      children: [
        header("Update Account", () => Navigator.pop(context)),
        const SizedBox(height: 20),
        field(Icons.person, "Full Name", name),
        field(Icons.cake, "Age", age),
        field(Icons.phone, "Phone no", phone),
        field(Icons.credit_card, "CNIC", cnic),
        field(Icons.attach_money, "Salary", salary),
        dropdown(),
        const Spacer(),
        Row(
          children: [
            Expanded(child: button("Home", Colors.blue, () {})),
            const SizedBox(width: 10),
            Expanded(
                child: button("Next", Colors.blue,
                    () => setState(() => step = 2))),
          ],
        )
      ],
    );
  }

  // ================= STEP 2 =================
  Widget stepTwo() {
    return Column(
      children: [
        header("Update Account", () => setState(() => step = 1)),
        const SizedBox(height: 20),
        
        // Image Picker Widget
        GestureDetector(
          onTap: _pickImage,
          child: Container(
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 10),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(25),
            ),
            child: Row(
              children: [
                Icon(_image == null ? Icons.image : Icons.check_circle, color: _image == null ? Colors.grey : Colors.green),
                const SizedBox(width: 15),
                Text(_image == null ? "Select Profile Picture" : "Picture Selected ✅"),
                const Spacer(),
                if (_image != null) 
                  CircleAvatar(radius: 15, backgroundImage: FileImage(_image!))
                else
                  const Icon(Icons.add_a_photo),
              ],
            ),
          ),
        ),

        field(Icons.email, "Email", email),
        field(Icons.home, "Address", address),

        // Skills Screen Navigation
        GestureDetector(
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (context) =>  AddSkillsScreen()),
            );
          },
          child: skillButton(),
        ),

        field(Icons.lock, "Password", password),
        field(Icons.lock, "Confirm Password", confirmPassword),

        const Spacer(),

        Row(
          children: [
            Expanded(
                child: button(
                    "Back", Colors.blue, () => setState(() => step = 1))),
            const SizedBox(width: 10),
            Expanded(child: button("Update", Colors.blue, () {
              // Submit Logic
              print("Profile updated");
            })),
          ],
        )
      ],
    );
  }

  // ================= COMMON WIDGETS =================

  Widget header(String title, VoidCallback onBack) {
    return Row(
      children: [
        IconButton(onPressed: onBack, icon: const Icon(Icons.arrow_back)),
        const SizedBox(width: 10),
        Text(title,
            style: const TextStyle(
                fontSize: 18, fontWeight: FontWeight.bold)),
      ],
    );
  }

  Widget field(IconData icon, String hint, TextEditingController controller) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.symmetric(horizontal: 15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(25),
        boxShadow: [
          BoxShadow(
              color: Colors.black.withOpacity(0.05),
              blurRadius: 5)
        ],
      ),
      child: TextField(
        controller: controller,
        decoration: InputDecoration(
          icon: Icon(icon, color: Colors.blue),
          hintText: hint,
          border: InputBorder.none,
        ),
      ),
    );
  }

  Widget dropdown() {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.symmetric(horizontal: 15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(25),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<String>(
          value: gender,
          isExpanded: true,
          icon: const Icon(Icons.arrow_drop_down, color: Colors.blue),
          items: ["Male", "Female"]
              .map((e) => DropdownMenuItem(value: e, child: Text(e)))
              .toList(),
          onChanged: (val) => setState(() => gender = val!),
        ),
      ),
    );
  }

  Widget skillButton() {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(25),
      ),
      child: Row(
        children: const [
          Icon(Icons.check_circle, color: Colors.green),
          SizedBox(width: 10),
          Text("Add Skills"),
          Spacer(),
          Icon(Icons.add, color: Colors.blue),
        ],
      ),
    );
  }

  Widget button(String text, Color color, VoidCallback onTap) {
    return ElevatedButton(
      style: ElevatedButton.styleFrom(
        backgroundColor: color,
        elevation: 0,
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(25)),
        padding: const EdgeInsets.symmetric(vertical: 15),
      ),
      onPressed: onTap,
      child: Text(text, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
    );
  }
}