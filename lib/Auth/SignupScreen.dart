import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

// API Configuration
class ApiConfig {
  // For Android Emulator
  static const String ipAddress = "10.0.2.2"; // Change this based on your setup
  
  // For Physical Device (same WiFi network)
  // static const String ipAddress = "192.168.0.103"; // Your actual PC IP
  
  static const String port = "5000"; // Updated to match running server
  static const String serverBase = "http://$ipAddress:$port";
  static const String apiAccount = '$serverBase/api/AccountCreation';
}

class SignupScreen extends StatefulWidget {
  final Map? initialData; // Edit ke waqt purana data lane ke liye
  final bool isEdit;
  final String? role;

  const SignupScreen({super.key, this.initialData, this.isEdit = false, this.role});

  @override
  _SignupScreenState createState() => _SignupScreenState();
}

class _SignupScreenState extends State<SignupScreen> {
  String role = 'Client';
  int step = 1;
  bool isLoading = false;

  // Controllers
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _ageController = TextEditingController();
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _cnicController = TextEditingController();
  final TextEditingController _salaryController = TextEditingController();
  final TextEditingController _emailController = TextEditingController();
  final TextEditingController _addressController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();
  final TextEditingController _confirmPasswordController = TextEditingController();
  final TextEditingController _bioController = TextEditingController();

  File? _selectedImage;
  String gender = 'Male';
  
  // Isme aap dynamic skills store karenge jo backend ko 'experiencesJson' ban kar jayenge
  List<Map<String, dynamic>> skillsData = []; 

  @override
  void initState() {
    super.initState();
    if (widget.isEdit && widget.initialData != null) {
      _setupEditMode();
    }
  }

  void _setupEditMode() {
    final data = widget.initialData!;
    setState(() {
      role = widget.role ?? 'Worker';
      _nameController.text = data['name'] ?? '';
      _phoneController.text = data['phone'] ?? '';
      _addressController.text = data['address'] ?? '';
      _emailController.text = data['email'] ?? '';
      _bioController.text = data['bio'] ?? '';
      _ageController.text = data['age']?.toString() ?? '';
      _cnicController.text = data['cnic'] ?? '';
      _salaryController.text = data['salary']?.toString() ?? '0';
      gender = data['gender'] ?? 'Male';
      // Note: Skills update mode mein purani skills backend se fetch karni hongi
    });
  }

  // --- API INTEGRATION LOGIC ---
  Future<void> _handleSignup() async {
    // Validation: Password match (Sirf naye signup ke waqt)
    if (!widget.isEdit && _passwordController.text != _confirmPasswordController.text) {
      _showMessage("Passwords do not match!", isError: true);
      return;
    }

    setState(() => isLoading = true);

    try {
      // 1. Endpoint chunna
      String endpoint = "";
      if (widget.isEdit) {
        endpoint = (role == 'Client') ? "UpdateClient" : "UpdateWorker";
      } else {
        endpoint = (role == 'Client') ? "SignupClient" : "SignupWorker";
      }

      var uri = Uri.parse("${ApiConfig.apiAccount}/$endpoint");
      var request = http.MultipartRequest('POST', uri);

      // 2. Common Fields Add karna
      if (widget.isEdit) {
        // Update ke liye ID lazmi hai (Client ya Worker)
        String idKey = (role == 'Client') ? "ClientId" : "WorkerId";
        request.fields[idKey] = widget.initialData!['id'].toString();
      }

      request.fields['Name'] = _nameController.text;
      request.fields['Phone'] = _phoneController.text;
      request.fields['Address'] = _addressController.text;
      request.fields['Email'] = _emailController.text;
      
      // Agar password khali hai to default star bhejte hain (Backend logic ke mutabiq)
      request.fields['Password'] = _passwordController.text.isEmpty ? "********" : _passwordController.text;

      // 3. Worker Specific Fields
      if (role == 'Worker') {
        request.fields['Cnic'] = _cnicController.text;
        request.fields['Age'] = _ageController.text;
        request.fields['Salary'] = _salaryController.text;
        request.fields['Gender'] = gender;
        request.fields['Bio'] = _bioController.text;
        
        // Agar koi skill select ki hai to JSON string bana kar bhejain
        request.fields['experiencesJson'] = jsonEncode(skillsData);
      }

      // 4. Image Upload
      if (_selectedImage != null) {
        request.files.add(await http.MultipartFile.fromPath(
          'PictureFile', // Backend model property name
          _selectedImage!.path,
          contentType: MediaType('image', 'jpeg'),
        ));
      }

      // 5. API Call Execute karna
      var streamedResponse = await request.send();
      var response = await http.Response.fromStream(streamedResponse);
      var result = jsonDecode(response.body);

      if (response.statusCode == 200) {
        _showMessage(result['message'] ?? "Operation Successful!", isError: false);
        
        if (!widget.isEdit) {
          Navigator.pushReplacementNamed(context, '/login');
        } else {
          Navigator.pop(context, true);
        }
      } else {
        _showMessage(result['message'] ?? "Error: ${response.statusCode}", isError: true);
      }
    } catch (e) {
      _showMessage("Connection Error: $e", isError: true);
    } finally {
      setState(() => isLoading = false);
    }
  }

  // --- HELPERS ---
  void _showMessage(String msg, {bool isError = false}) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(msg), backgroundColor: isError ? Colors.red : Colors.green),
    );
  }

  Future<void> _pickImage() async {
    final pickedFile = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 50);
    if (pickedFile != null) setState(() => _selectedImage = File(pickedFile.path));
  }

  // --- UI BUILDING ---
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => step == 2 ? setState(() => step = 1) : Navigator.pop(context),
        ),
        title: Text(widget.isEdit ? 'Edit Profile' : 'Create Account', 
            style: const TextStyle(color: Colors.black, fontWeight: FontWeight.bold)),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            if (!widget.isEdit) _buildRoleSelector(),
            const SizedBox(height: 20),
            if (role == 'Client' || (role == 'Worker' && step == 1)) _buildFormStep1(),
            if (role == 'Worker' && step == 2) _buildFormStep2(),
          ],
        ),
      ),
    );
  }

  Widget _buildRoleSelector() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        _roleCard("Client", Icons.person_outline, role == 'Client'),
        _roleCard("Worker", Icons.engineering_outlined, role == 'Worker'),
      ],
    );
  }

  Widget _roleCard(String title, IconData icon, bool isActive) {
    return GestureDetector(
      onTap: () => setState(() { role = title; step = 1; }),
      child: Container(
        width: MediaQuery.of(context).size.width * 0.43,
        padding: const EdgeInsets.symmetric(vertical: 15),
        decoration: BoxDecoration(
          color: isActive ? const Color(0xFFE0DADA) : Colors.white,
          borderRadius: BorderRadius.circular(15),
          border: Border.all(color: isActive ? const Color(0xFF1E64D3) : Colors.grey.shade200),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: isActive ? const Color(0xFF1E64D3) : Colors.black54),
            const SizedBox(width: 8),
            Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
      ),
    );
  }

  Widget _buildFormStep1() {
    return Column(
      children: [
        _buildAvatarPicker(),
        const SizedBox(height: 20),
        _customInput("Full Name", Icons.person_outline, _nameController),
        if (role == 'Worker') ...[
          _customInput("Age", Icons.calendar_today, _ageController, type: TextInputType.number),
          _customInput("CNIC", Icons.badge_outlined, _cnicController, type: TextInputType.number),
          _customInput("Salary (Expected)", Icons.attach_money, _salaryController, type: TextInputType.number),
        ],
        _customInput("Phone no", Icons.phone_android, _phoneController, type: TextInputType.phone),
        _customInput("Address", Icons.location_on_outlined, _addressController),
        if (role == 'Client') ...[
          _customInput("Email", Icons.email_outlined, _emailController),
          _customInput("Password", Icons.lock_outline, _passwordController, isPass: true),
          _customInput("Confirm Password", Icons.lock_outline, _confirmPasswordController, isPass: true),
        ],
        const SizedBox(height: 30),
        _buildActionButtons(
          role == 'Client' ? (widget.isEdit ? "Update" : "Signup") : "Next",
          () => role == 'Client' ? _handleSignup() : setState(() => step = 2),
        ),
      ],
    );
  }

  Widget _buildFormStep2() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text("PROFESSIONAL DESCRIPTION", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
        const SizedBox(height: 10),
        _customInput("Bio...", Icons.description_outlined, _bioController, maxLines: 3),
        _customInput("Email", Icons.email_outlined, _emailController),
        
        // Skills Section (Sample Add)
        _customButtonInput("Add Skill (Mock)", Icons.add_circle_outline, const Color(0xFF1E64D3), () {
           setState(() {
             skillsData.add({
               "CategoryId": 1, // Isse UI se dynamic karna hai
               "SkillsId": 1,
               "ExperienceYears": "2"
             });
           });
           _showMessage("Skill added to list");
        }),
        
        const Text("SELECT GENDER", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
        Row(
          children: [
            _genderChip("Male", Icons.male),
            _genderChip("Female", Icons.female),
          ],
        ),
        _customInput("Password", Icons.lock_outline, _passwordController, isPass: true),
        _customInput("Confirm Password", Icons.lock_outline, _confirmPasswordController, isPass: true),
        const SizedBox(height: 30),
        _buildActionButtons(widget.isEdit ? "Update Profile" : "Submit", _handleSignup),
      ],
    );
  }

  // --- REUSABLE UI WIDGETS ---
  Widget _buildAvatarPicker() {
    return GestureDetector(
      onTap: _pickImage,
      child: Container(
        height: 110, width: 110,
        decoration: BoxDecoration(
          color: Colors.grey.shade100,
          shape: BoxShape.circle,
          border: Border.all(color: Colors.grey.shade300),
          image: _selectedImage != null 
              ? DecorationImage(image: FileImage(_selectedImage!), fit: BoxFit.cover) 
              : null,
        ),
        child: _selectedImage == null ? const Icon(Icons.camera_enhance_outlined, size: 30, color: Colors.grey) : null,
      ),
    );
  }

  Widget _customInput(String hint, IconData icon, TextEditingController ctrl, {bool isPass = false, TextInputType type = TextInputType.text, int maxLines = 1}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10)],
      ),
      child: TextField(
        controller: ctrl,
        obscureText: isPass,
        keyboardType: type,
        maxLines: maxLines,
        decoration: InputDecoration(
          hintText: hint,
          prefixIcon: Icon(icon, color: Colors.black87),
          border: InputBorder.none,
          contentPadding: const EdgeInsets.all(15),
        ),
      ),
    );
  }

  Widget _customButtonInput(String text, IconData icon, Color color, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 15),
        padding: const EdgeInsets.all(15),
        decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(15), boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10)]),
        child: Row(
          children: [
            Icon(icon, color: color),
            const SizedBox(width: 15),
            Text(text),
            const Spacer(),
            const Icon(Icons.chevron_right, color: Colors.black54),
          ],
        ),
      ),
    );
  }

  Widget _genderChip(String label, IconData icon) {
    bool isSelected = gender == label;
    return Expanded(
      child: GestureDetector(
        onTap: () => setState(() => gender = label),
        child: Container(
          margin: const EdgeInsets.all(5),
          padding: const EdgeInsets.symmetric(vertical: 12),
          decoration: BoxDecoration(
            color: isSelected ? const Color(0xFF1E64D3) : Colors.grey.shade100,
            borderRadius: BorderRadius.circular(15),
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: isSelected ? Colors.white : Colors.black, size: 18),
              const SizedBox(width: 5),
              Text(label, style: TextStyle(color: isSelected ? Colors.white : Colors.black, fontWeight: FontWeight.bold)),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildActionButtons(String mainText, VoidCallback onMainPress) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        _btn("Back", Colors.grey.shade200, Colors.black, () => step == 2 ? setState(() => step = 1) : Navigator.pop(context)),
        _btn(mainText, const Color(0xFF1E64D3), Colors.white, onMainPress),
      ],
    );
  }

  Widget _btn(String text, Color bg, Color txt, VoidCallback onPress) {
    return SizedBox(
      width: MediaQuery.of(context).size.width * 0.42,
      height: 55,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPress,
        style: ElevatedButton.styleFrom(backgroundColor: bg, elevation: 0, shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30))),
        child: isLoading && text != "Back" 
            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) 
            : Text(text, style: TextStyle(color: txt, fontSize: 16, fontWeight: FontWeight.bold)),
      ),
    );
  }
}