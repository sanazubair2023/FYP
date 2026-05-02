import 'package:flutter/material.dart';
import 'dart:convert'; // JSON encoding/decoding ke liye
import 'package:http/http.dart' as http; // API calls ke liye
import 'SignupScreen.dart';
import '../Client/FindServiceScreen.dart';
import '../Worker/WorkerDashboard.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  // CONFIGURATION: Apne PC ka IP yahan likhein
  // For Android Emulator
  final String baseUrl = "http://10.0.2.2:5000/api"; 
  
  // For Physical Device (same WiFi network)
  // final String baseUrl = "http://192.168.0.103:5000/api"; 

  String _role = 'Client'; 
  bool _isPasswordVisible = false;
  bool _rememberMe = false;
  bool _isLoading = false;

  final TextEditingController _emailCnicController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();

  // --- API INTEGRATION LOGIC ---
  Future<void> _handleLogin() async {
    final input = _emailCnicController.text.trim();
    final password = _passwordController.text.trim();

    if (input.isEmpty || password.isEmpty) {
      _showSnackBar("Please enter your credentials.");
      return;
    }

    setState(() => _isLoading = true);

    try {
      // Backend ke 'LoginDto' ke mutabiq data map
      final Map<String, dynamic> loginData = {
        "role": _role,
        "emailOrCnic": input,
        "password": password,
      };

      final response = await http.post(
        Uri.parse("$baseUrl/Auth/Login"),
        headers: {"Content-Type": "application/json"},
        body: jsonEncode(loginData),
      );

      if (response.statusCode == 200) {
        final responseData = jsonDecode(response.body);
        
        // Success: Token aur Details mil jayengi[cite: 1, 2]
        debugPrint("Login Success: ${responseData['token']}");

        // Navigation based on Role
        if (_role == 'Client') {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (context) => const FindServiceScreen()),
          );
        } else {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (context) => const WorkerDashboard()),
          );
        }
      } else {
        // Error: Backend se message dikhayein
        final errorData = jsonDecode(response.body);
        _showSnackBar(errorData['message'] ?? "Invalid Credentials");
      }
    } catch (e) {
      // Connection Error (IP ya Network ka masla)[cite: 2]
      _showSnackBar("Connection failed! Check if backend is running on $baseUrl");
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _showSnackBar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.redAccent),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FBFF),
      body: Stack(
        children: [
          // Background Circle
          Positioned(
            top: -40, left: -40,
            child: Container(
              width: 180, height: 180,
              decoration: const BoxDecoration(color: Color(0xFFD6EAF8), shape: BoxShape.circle),
            ),
          ),
          SafeArea(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 30),
              child: Column(
                children: [
                  const SizedBox(height: 40),
                  // Logo Section
                  _buildLogo(),
                  const SizedBox(height: 40),
                  // Role Selection
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _buildRoleButton("Client", Icons.person_outline),
                      _buildRoleButton("Worker", Icons.group_outlined),
                    ],
                  ),
                  const SizedBox(height: 20),
                  // Inputs
                  _buildInputWrapper(
                    child: TextField(
                      controller: _emailCnicController,
                      keyboardType: _role == 'Client' ? TextInputType.emailAddress : TextInputType.number,
                      decoration: InputDecoration(
                        border: InputBorder.none,
                        hintText: _role == 'Client' ? "Email" : "CNIC (No Dashes)",
                        icon: Icon(_role == 'Client' ? Icons.email_outlined : Icons.badge_outlined, color: const Color(0xFF1E64D3)),
                      ),
                    ),
                  ),
                  const SizedBox(height: 15),
                  _buildInputWrapper(
                    child: TextField(
                      controller: _passwordController,
                      obscureText: !_isPasswordVisible,
                      decoration: InputDecoration(
                        border: InputBorder.none,
                        hintText: "Password",
                        icon: const Icon(Icons.lock_outline, color: Color(0xFF1E64D3)),
                        suffixIcon: IconButton(
                          icon: Icon(_isPasswordVisible ? Icons.visibility_off : Icons.visibility),
                          onPressed: () => setState(() => _isPasswordVisible = !_isPasswordVisible),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 15),
                  // Remember Me
                  _buildRememberMe(),
                  const SizedBox(height: 25),
                  // Sign In Button
                  SizedBox(
                    width: double.infinity, height: 55,
                    child: OutlinedButton(
                      onPressed: _isLoading ? null : _handleLogin,
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: Color(0xFF1E64D3)),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
                      ),
                      child: _isLoading 
                        ? const CircularProgressIndicator() 
                        : const Text("Sign in", style: TextStyle(fontSize: 18)),
                    ),
                  ),
                  const Padding(padding: EdgeInsets.symmetric(vertical: 15), child: Text("OR")),
                  // Signup Button
                  _buildSignupButton(),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // --- Helper Widgets ---

  Widget _buildLogo() {
    return Column(
      children: [
        Container(
          width: 120, height: 120,
          decoration: BoxDecoration(
            color: Colors.white, borderRadius: BorderRadius.circular(25),
            boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.1), blurRadius: 10, offset: const Offset(0, 5))],
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(25),
            child: Image.asset('assets/images/logo.png', fit: BoxFit.contain, errorBuilder: (c, e, s) => const Icon(Icons.broken_image, size: 50)),
          ),
        ),
        const SizedBox(height: 15),
        const Text("Maid & Servant Online", style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Color(0xFF2C437E))),
      ],
    );
  }

  Widget _buildRoleButton(String title, IconData icon) {
    bool isActive = _role == title;
    return GestureDetector(
      onTap: () => setState(() => _role = title),
      child: Container(
        width: MediaQuery.of(context).size.width * 0.4,
        height: 50,
        decoration: BoxDecoration(
          color: isActive ? const Color(0xFFE0DADA) : Colors.white,
          borderRadius: BorderRadius.circular(15),
          border: isActive ? Border.all(color: const Color(0xFF1E64D3), width: 1) : null,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: isActive ? const Color(0xFF1E64D3) : Colors.grey),
            const SizedBox(width: 10),
            Text(title, style: TextStyle(fontWeight: isActive ? FontWeight.bold : FontWeight.normal, color: isActive ? const Color(0xFF1E64D3) : Colors.black)),
          ],
        ),
      ),
    );
  }

  Widget _buildInputWrapper({required Widget child}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 15),
      decoration: BoxDecoration(
        color: Colors.white, borderRadius: BorderRadius.circular(15),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 5, offset: const Offset(0, 2))],
      ),
      child: child,
    );
  }

  Widget _buildRememberMe() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        GestureDetector(
          onTap: () => setState(() => _rememberMe = !_rememberMe),
          child: Row(
            children: [
              Icon(_rememberMe ? Icons.check_box : Icons.check_box_outline_blank, color: const Color(0xFF1E64D3)),
              const SizedBox(width: 8),
              const Text("Remember Me"),
            ],
          ),
        ),
        TextButton(onPressed: () {}, child: const Text("Forgot Password?")),
      ],
    );
  }

  Widget _buildSignupButton() {
    return SizedBox(
      width: double.infinity, height: 55,
      child: ElevatedButton(
        onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (context) => SignupScreen())),
        style: ElevatedButton.styleFrom(
          backgroundColor: const Color(0xFF1E64D3),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
        ),
        child: const Text("Signup", style: TextStyle(fontSize: 18, color: Colors.white)),
      ),
    );
  }
}