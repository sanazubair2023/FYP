import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart'; 

class Editclientprofile extends StatefulWidget {
  const Editclientprofile({super.key});

  @override
  State<Editclientprofile> createState() => _EditclientprofileState();
}

class _EditclientprofileState extends State<Editclientprofile> {
  bool _isPasswordVisible = false;
  bool _isConfirmPasswordVisible = false;
  
  File? _image;
  final ImagePicker _picker = ImagePicker();

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
      backgroundColor: Colors.white,
      body: SafeArea(
        child: SingleChildScrollView(
          child: Column(
            children: [
              // --- Header ---
              _buildHeader(context),

              // --- Logo Section ---
              const SizedBox(height: 10),
              Center(
                child: Image.asset(
                 'assets/images/logo.png', 
                  height: 100,
                  width: 100,
                  fit: BoxFit.contain,
                ),
              ),
              const SizedBox(height: 10),
              
              const Text(
                "Update Your Profile",
                style: TextStyle(
                  fontSize: 18, 
                  color: Colors.grey, 
                  fontWeight: FontWeight.w500
                ),
              ),
              const SizedBox(height: 25),

              // --- Form Fields ---
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 25),
                child: Column(
                  children: [
                    _buildTextField(hint: "Full Name", icon: Icons.person_outline),
                    _buildTextField(hint: "Email", icon: Icons.email_outlined),
                    _buildTextField(hint: "Phone no", icon: Icons.phone_outlined),

                    // Image Picker Field
                    GestureDetector(
                      onTap: _pickImage,
                      child: Container(
                        margin: const EdgeInsets.only(bottom: 20),
                        padding: const EdgeInsets.symmetric(vertical: 15, horizontal: 20),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(30),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.1), 
                              blurRadius: 10, 
                              offset: const Offset(0, 5)
                            ),
                          ],
                        ),
                        child: Row(
                          children: [
                            Icon(
                              _image == null ? Icons.image_outlined : Icons.check_circle, 
                              color: _image == null ? Colors.black87 : Colors.green
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                _image == null ? "Select Profile Picture" : "Picture Selected",
                                style: TextStyle(
                                  color: _image == null ? Colors.grey : Colors.black, 
                                  fontSize: 16
                                ),
                              ),
                            ),
                            if (_image != null)
                              ClipRRect(
                                borderRadius: BorderRadius.circular(8),
                                child: Image.file(_image!, width: 40, height: 40, fit: BoxFit.cover),
                              ),
                          ],
                        ),
                      ),
                    ),

                    _buildTextField(hint: "Address", icon: Icons.home_outlined),
                    
                    _buildTextField(
                      hint: "New Password", // یہاں بھی تھوڑی تبدیلی کی ہے
                      icon: Icons.lock_outline,
                      isPassword: true,
                      isVisible: _isPasswordVisible,
                      onToggle: () => setState(() => _isPasswordVisible = !_isPasswordVisible),
                    ),

                    _buildTextField(
                      hint: "Confirm New Password",
                      icon: Icons.lock_outline,
                      isPassword: true,
                      isVisible: _isConfirmPasswordVisible,
                      onToggle: () => setState(() => _isConfirmPasswordVisible = !_isConfirmPasswordVisible),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 20),

              // --- Buttons ---
              _buildButtons(context),
            ],
          ),
        ),
      ),
    );
  }

  // --- Widgets ---

  Widget _buildHeader(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(20.0),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.arrow_back_ios, color: Colors.black), 
            onPressed: () => Navigator.pop(context)
          ),
          const Expanded(
            child: Text(
              "Update Account", // 👈 یہاں تبدیلی کی گئی ہے
              textAlign: TextAlign.center, 
              style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold)
            )
          ),
          const SizedBox(width: 40), 
        ],
      ),
    );
  }

  Widget _buildTextField({
    required String hint, 
    required IconData icon, 
    bool isPassword = false, 
    bool isVisible = false, 
    VoidCallback? onToggle
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(30),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1), 
            blurRadius: 10, 
            offset: const Offset(0, 5)
          )
        ],
      ),
      child: TextField(
        obscureText: isPassword && !isVisible,
        decoration: InputDecoration(
          hintText: hint,
          prefixIcon: Icon(icon, color: Colors.black87),
          suffixIcon: isPassword 
              ? IconButton(
                  icon: Icon(isVisible ? Icons.visibility : Icons.visibility_off), 
                  onPressed: onToggle
                ) 
              : null,
          border: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(vertical: 18, horizontal: 20),
        ),
      ),
    );
  }

  Widget _buildButtons(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 25, vertical: 20),
      child: Row(
        children: [
          Expanded(child: _button("Cancel", () => Navigator.pop(context), isPrimary: false)),
          const SizedBox(width: 20),
          Expanded(child: _button("Save Changes", () {}, isPrimary: true)),
        ],
      ),
    );
  }

  Widget _button(String label, VoidCallback onTap, {bool isPrimary = true}) {
    return ElevatedButton(
      onPressed: onTap,
      style: ElevatedButton.styleFrom(
        backgroundColor: isPrimary ? const Color(0xFF1E64D3) : Colors.grey.shade200,
        elevation: isPrimary ? 2 : 0,
        padding: const EdgeInsets.symmetric(vertical: 15),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
      ),
      child: Text(
        label, 
        style: TextStyle(
          color: isPrimary ? Colors.white : Colors.black87, 
          fontSize: 16, 
          fontWeight: FontWeight.bold
        )
      ),
    );
  }
}