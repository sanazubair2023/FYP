import 'package:flutter/material.dart';

// AUTH
import 'Auth/LoginScreen.dart';
import 'Auth/SignupScreen.dart';

// CLIENT
import 'Client/FindServiceScreen.dart';
import 'Client/UserDashboardScreen.dart';
import 'Client/DateAndTime.dart';
import 'Client/FilterScreen.dart';
import 'Client/WorkerDetailScreen.dart'; 
import 'Client/EditClientProfile.dart'; 
import 'Client/ActiveRequestScreen.dart';
import 'Client/ResignationScreen.dart'; 
import 'Client/ResignationsListScreen.dart'; 
import 'Client/RatingAndReviewsScreen.dart';
import 'Client/WorkerDecisionScreen.dart'; 

// WORKER
import 'Worker/AddSkillsScreen.dart';
import 'Worker/WorkerDashboard.dart';
import 'Worker/EditWorkerProfile.dart'; 

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Servant Maid Online',
      theme: ThemeData(
        primaryColor: const Color(0xFF1E64D3), 
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF1E64D3)),
      ),

      home:  LoginScreen(),

      routes: {
        // --- AUTH ROUTES ---
        '/login': (context) =>  LoginScreen(),
        '/signup': (context) =>  SignupScreen(),
        
        // --- CLIENT ROUTES ---
        '/searchworker': (context) => const FindServiceScreen(),
        '/clientprofile': (context) => const UserDashboardScreen(),
        '/workerdecision': (context) => const WorkerDecisionScreen(), 
        
        // Date and Time Route
        '/datetime': (context) {
          final args = ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>;
          return DateAndTime(
            workerId: args['workerId'],
            workerName: args['workerName'],
          );
        },

        '/filterscreen': (context) => const FilterScreen(),
        '/editclientprofile': (context) => const Editclientprofile(),
        '/activerequests': (context) => const ActiveRequestScreen(), 
        '/resignationslist': (context) => const ResignationsListScreen(),

        // NEW: Rating and Reviews Route
        '/reviews': (context) {
          final args = ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>;
          return RatingAndReviewsScreen(
            workerId: args['workerId'],
            initialRating: args['initialRating'],
            initialReviewCount: args['initialReviewCount'],
          );
        },
        
        // Worker Detail Route
        '/workerdetail': (context) {
          final args = ModalRoute.of(context)!.settings.arguments as String;
          return WorkerDetailScreen(workerId: args);
        },

        // Single Resignation Detail Route
        '/resignation': (context) {
          final args = ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>;
          return ResignationScreen(
            resignationId: args['resignationId'],
          );
        },

        // --- WORKER ROUTES ---
        '/addskills': (context) => AddSkillsScreen(),
        '/workerdashboard': (context) => const WorkerDashboard(),
        '/editworkerprofile': (context) => const EditWorkerProfile(),
      },
    );
  }
}