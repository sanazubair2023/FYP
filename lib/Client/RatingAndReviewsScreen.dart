import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

// Maaun ke URL ko yahan config se replace karein
const String API_DASHBOARD = "https://your-api-url.com/api/Dashboard";

class RatingAndReviewsScreen extends StatefulWidget {
  final String workerId;
  final String? initialRating;
  final int? initialReviewCount;

  const RatingAndReviewsScreen({
    super.key,
    required this.workerId,
    this.initialRating,
    this.initialReviewCount,
  });

  @override
  State<RatingAndReviewsScreen> createState() => _RatingAndReviewsScreenState();
}

class _RatingAndReviewsScreenState extends State<RatingAndReviewsScreen> {
  List<dynamic> reviews = [];
  String averageRating = "0.0";
  int reviewCount = 0;
  bool isLoading = true;

  @override
  void initState() {
    super.initState();
    averageRating = widget.initialRating ?? "0.0";
    reviewCount = widget.initialReviewCount ?? 0;
    fetchReviews();
  }

  // -------------------- API Call --------------------
  Future<void> fetchReviews() async {
    setState(() => isLoading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      final response = await http.get(
        Uri.parse('$API_DASHBOARD/GetWorkerReviews/${widget.workerId}'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        setState(() {
          reviews = data['reviews'] ?? [];
          averageRating = data['averageRating']?.toString() ?? "0.0";
          reviewCount = data['reviewCount'] ?? 0;
        });
      } else {
        debugPrint("Failed to fetch reviews");
      }
    } catch (error) {
      debugPrint("Error fetching reviews: $error");
    } finally {
      setState(() => isLoading = false);
    }
  }

  // -------------------- UI Helper Methods --------------------
  List<Widget> _renderStars(double rating, {double size = 18}) {
    List<Widget> stars = [];
    for (int i = 1; i <= 5; i++) {
      stars.add(
        Icon(
          Icons.star,
          size: size,
          color: i <= rating.round() ? const Color(0xFFFFD700) : const Color(0xFFE0E0E0),
        ),
      );
    }
    return stars;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text("Ratings & Reviews", style: TextStyle(color: Colors.black)),
      ),
      body: isLoading
          ? _buildLoader()
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  _buildHeaderCard(),
                  const SizedBox(height: 10),
                  reviews.isEmpty ? _buildEmptyState() : _buildReviewsList(),
                  const SizedBox(height: 20),
                  _buildBackKey(),
                ],
              ),
            ),
    );
  }

  // -------------------- UI Sections --------------------

  Widget _buildLoader() {
    return const Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          CircularProgressIndicator(color: Color(0xFF1E75EB)),
          SizedBox(height: 10),
          Text("Loading reviews...", style: TextStyle(color: Color(0xFF1E75EB))),
        ],
      ),
    );
  }

  Widget _buildHeaderCard() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(25),
        border: Border.all(color: const Color(0xFFE0E0E0)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text("Overall Rating", style: TextStyle(fontSize: 18, color: Color(0xFF333333))),
          const SizedBox(height: 10),
          Row(
            children: [
              Text(
                averageRating,
                style: const TextStyle(fontSize: 48, fontWeight: FontWeight.bold, color: Color(0xFF1E4A84)),
              ),
              const SizedBox(width: 15),
              Row(children: _renderStars(double.parse(averageRating), size: 30)),
            ],
          ),
          Text("($reviewCount Reviews)", style: const TextStyle(fontSize: 16, color: Colors.grey)),
        ],
      ),
    );
  }

  Widget _buildReviewsList() {
    return Column(
      children: reviews.map((item) {
        return Container(
          margin: const EdgeInsets.only(bottom: 15),
          padding: const EdgeInsets.all(15),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(15),
            border: Border.all(color: const Color(0xFFE0E0E0)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item['name'] ?? "", style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      Row(
                        children: [
                          const Icon(Icons.calendar_today, size: 14, color: Colors.grey),
                          const SizedBox(width: 4),
                          Text(item['date'] ?? "", style: const TextStyle(fontSize: 12, color: Colors.grey)),
                        ],
                      ),
                    ],
                  ),
                  Row(children: _renderStars(double.parse(item['rating'].toString()))),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                "\"${item['comment']}\"",
                style: const TextStyle(fontSize: 13, color: Color(0xFF555555), fontStyle: FontStyle.italic, height: 1.4),
              ),
            ],
          ),
        );
      }).toList(),
    );
  }

  Widget _buildEmptyState() {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 50),
      child: Column(
        children: [
          Icon(Icons.message_outlined, size: 60, color: Colors.grey[300]),
          const SizedBox(height: 10),
          const Text("No reviews available yet.", style: TextStyle(color: Colors.grey, fontStyle: FontStyle.italic)),
        ],
      ),
    );
  }

  Widget _buildBackKey() {
    return SizedBox(
      width: double.infinity,
      height: 50,
      child: ElevatedButton(
        onPressed: () => Navigator.pop(context),
        style: ElevatedButton.styleFrom(
          backgroundColor: const Color(0xFF1E75EB),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(25)),
          elevation: 3,
        ),
        child: const Text("Back", style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
      ),
    );
  }
}