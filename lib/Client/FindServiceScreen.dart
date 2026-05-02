import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:cached_network_image/cached_network_image.dart';


const String SERVER_BASE = "YOUR_SERVER_BASE_URL";
const String API_BASE = "YOUR_API_DASHBOARD_URL";

class FindServiceScreen extends StatefulWidget {
  final Map<String, dynamic>? appliedFilters;

  const FindServiceScreen({super.key, this.appliedFilters});

  @override
  State<FindServiceScreen> createState() => _FindServiceScreenState();
}

class _FindServiceScreenState extends State<FindServiceScreen> {
  String search = '';
  List<String> categoriesList = ['All'];
  String selectedCategory = 'All';
  List<dynamic> workers = [];
  bool isLoading = true;
  String clientName = 'Client';
  String clientPicture = '';
  Timer? _debounce;

  Map<String, dynamic> allFilters = {
    'gender': '',
    'city': '',
    'categories': [],
    'subSkills': {} 
  };

  @override
  void initState() {
    super.initState();
    _loadUserData();
    fetchCategories();
    
    if (widget.appliedFilters != null) {
      allFilters = widget.appliedFilters!;
    }
    
    fetchWorkers();
  }

  _loadUserData() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      clientName = prefs.getString('userName') ?? 'Client';
      clientPicture = prefs.getString('userPicture') ?? '';
    });
  }


  Future<void> fetchCategories() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');
      
      final response = await http.get(
        Uri.parse('$API_BASE/GetFiltersData'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        List<dynamic> data = json.decode(response.body);
        setState(() {
          categoriesList = ['All', ...data.map((c) => c['categoryName'].toString())];
        });
      }
    } catch (e) {
      print("Failed to fetch categories: $e");
    }
  }

  
  Future<void> fetchWorkers() async {
    setState(() => isLoading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      String? token = prefs.getString('userToken');

      // Query String بنانا
      Map<String, String> queryParams = {};
      
      if (search.trim().isNotEmpty) {
        queryParams['search'] = search.trim();
      }

      
      List<String> combinedCats = List<String>.from(allFilters['categories']);
      if (selectedCategory != 'All' && !combinedCats.contains(selectedCategory)) {
        combinedCats.add(selectedCategory);
      }

      String url = '$API_BASE/GetWorkersForClient?';
      for (var cat in combinedCats) {
        url += 'categories=${Uri.encodeComponent(cat)}&';
      }

      if (allFilters['gender'] != null && allFilters['gender'] != 'Both') {
        url += 'gender=${Uri.encodeComponent(allFilters['gender'])}&';
      }

      if (allFilters['city'] != null && allFilters['city'].isNotEmpty) {
        url += 'city=${Uri.encodeComponent(allFilters['city'])}&';
      }

      // Sub-skills Logic
      Map<String, dynamic> subSkills = allFilters['subSkills'];
      subSkills.forEach((key, value) {
        for (var skill in value) {
          url += 'subSkills=${Uri.encodeComponent(skill)}&';
        }
      });

      final response = await http.get(
        Uri.parse(url + (search.isNotEmpty ? 'search=${Uri.encodeComponent(search)}' : '')),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          workers = json.decode(response.body);
        });
      } else if (response.statusCode == 401) {
        // Session Expired Logic
        Navigator.pushReplacementNamed(context, '/login');
      }
    } catch (e) {
      print("Network error: $e");
    } finally {
      setState(() => isLoading = false);
    }
  }

  // سرچ ڈی باؤنس (Optimization)
  void _onSearchChanged(String value) {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      setState(() => search = value);
      fetchWorkers();
    });
  }

  String extractCity(String? address) {
    if (address == null || address == 'N/A') return 'N/A';
    List<String> parts = address.split(',');
    return parts.length > 1 ? parts.last.trim() : address.trim();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Column(
          children: [
            _buildHeader(),
            _buildSearchBar(),
            _buildCategoryTabs(),
            _buildFilterSortRow(),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
              child: Align(
                alignment: Alignment.centerRight,
                child: Text("${workers.length} Total Results", 
                  style: const TextStyle(fontSize: 12, color: Colors.grey)),
              ),
            ),
            Expanded(
              child: isLoading 
                ? const Center(child: CircularProgressIndicator(color: Color(0xFF1E64D3)))
                : _buildWorkersList(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Padding(
      padding: const EdgeInsets.all(20),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  GestureDetector(
                    onTap: () => Navigator.pop(context),
                    child: Container(
                      padding: const EdgeInsets.all(5),
                      decoration: BoxDecoration(color: Colors.grey[200], shape: BoxShape.circle),
                      child: const Icon(Icons.arrow_back, size: 24, color: Colors.black54),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Text("Welcome, $clientName", 
                    style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Color(0xFF001F3F))),
                ],
              ),
              const SizedBox(height: 5),
              const Text("FIND SERVICE", style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
              const Text("What would you like to do?", style: TextStyle(fontSize: 12, color: Colors.grey)),
            ],
          ),
          GestureDetector(
            onTap: () => Navigator.pushNamed(context, '/clientprofile'),
            child: Stack(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(30),
                  child: CachedNetworkImage(
                    imageUrl: clientPicture.startsWith('/') ? "$SERVER_BASE$clientPicture" : "https://cdn-icons-png.flaticon.com/512/3135/3135768.png",
                    width: 55, height: 55, fit: BoxFit.cover,
                    placeholder: (context, url) => Container(color: Colors.grey[200]),
                  ),
                ),
                Positioned(
                  bottom: 0, right: 0,
                  child: Container(
                    padding: const EdgeInsets.all(2),
                    decoration: BoxDecoration(color: Colors.grey[100], shape: BoxShape.circle, border: Border.all(color: Colors.white)),
                    child: const Icon(Icons.person, size: 14),
                  ),
                )
              ],
            ),
          )
        ],
      ),
    );
  }

  Widget _buildSearchBar() {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
      padding: const EdgeInsets.symmetric(horizontal: 15),
      decoration: BoxDecoration(
        color: Colors.white, borderRadius: BorderRadius.circular(15),
        border: Border.all(color: Colors.grey[300]!),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10)]
      ),
      child: TextField(
        onChanged: _onSearchChanged,
        decoration: const InputDecoration(
          hintText: "Search by Name",
          border: InputBorder.none,
          suffixIcon: Icon(Icons.search, color: Colors.black87),
        ),
      ),
    );
  }

  Widget _buildCategoryTabs() {
    return SizedBox(
      height: 45,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.only(left: 15),
        itemCount: categoriesList.length,
        itemBuilder: (context, index) {
          String cat = categoriesList[index];
          bool isSelected = selectedCategory == cat;
          return GestureDetector(
            onTap: () {
              setState(() => selectedCategory = cat);
              fetchWorkers();
            },
            child: Container(
              margin: const EdgeInsets.only(right: 8),
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
              decoration: BoxDecoration(
                color: isSelected ? const Color(0xFF1E64D3) : Colors.grey[100],
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: isSelected ? const Color(0xFF1E64D3) : Colors.grey[300]!),
              ),
              child: Center(
                child: Text(cat, style: TextStyle(color: isSelected ? Colors.white : Colors.black, fontWeight: FontWeight.bold)),
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildFilterSortRow() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 15),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
        children: [
          _filterButton("Filter", Icons.list, () async {
            final result = await Navigator.pushNamed(context, '/filterscreen', arguments: allFilters);
            if (result != null) {
              setState(() => allFilters = result as Map<String, dynamic>);
              fetchWorkers();
            }
          }),
          _filterButton("Sort", Icons.arrow_upward, () {}),
        ],
      ),
    );
  }

  Widget _filterButton(String title, IconData icon, VoidCallback onTap) {
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 30, vertical: 10),
        decoration: BoxDecoration(color: Colors.grey[100], borderRadius: BorderRadius.circular(12), border: Border.all(color: Colors.grey[300]!)),
        child: Row(
          children: [
            Text(title, style: const TextStyle(fontWeight: FontWeight.w500)),
            const SizedBox(width: 8),
            Icon(icon, size: 18, color: Colors.grey[600]),
          ],
        ),
      ),
    );
  }

  Widget _buildWorkersList() {
    if (workers.isEmpty) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.person_search_outlined, size: 60, color: Colors.grey),
            Text("No workers found", style: TextStyle(color: Colors.grey, fontSize: 16)),
          ],
        ),
      );
    }
    return ListView.builder(
      padding: const EdgeInsets.symmetric(horizontal: 20),
      itemCount: workers.length,
      itemBuilder: (context, index) => _buildWorkerCard(workers[index]),
    );
  }

  Widget _buildWorkerCard(dynamic worker) {
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white, borderRadius: BorderRadius.circular(24),
        border: Border.all(color: Colors.grey[100]!),
        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10, offset: const Offset(0, 4))]
      ),
      child: Column(
        children: [
          Row(
            children: [
              Stack(
                clipBehavior: Clip.none,
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(20),
                    child: CachedNetworkImage(
                      imageUrl: worker['picture'] != null && worker['picture'].startsWith('/') 
                        ? "$SERVER_BASE${worker['picture']}" 
                        : 'assets/images/logo1.png',
                      width: 85, height: 85, fit: BoxFit.cover,
                    ),
                  ),
                  Positioned(
                    bottom: -5, right: -5,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(8), boxShadow: [BoxShadow(color: Colors.black12, blurRadius: 4)]),
                      child: Row(
                        children: [
                          const Icon(Icons.star, size: 12, color: Colors.orange),
                          Text(worker['rating']?.toString() ?? "0.0", style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold)),
                        ],
                      ),
                    ),
                  )
                ],
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(child: Text(worker['name'] ?? '', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold), maxLines: 1)),
                        Text(worker['salary'] ?? 'N/A', style: const TextStyle(color: Colors.green, fontWeight: FontWeight.bold)),
                      ],
                    ),
                    Text(worker['role'] ?? '', style: const TextStyle(color: Colors.grey, fontSize: 13)),
                    const SizedBox(height: 5),
                    Row(
                      children: [
                        const Icon(Icons.location_on_outlined, size: 14, color: Colors.grey),
                        Text(extractCity(worker['city']), style: const TextStyle(fontSize: 12, color: Colors.grey)),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6,
                      children: (worker['categories'] as List? ?? []).take(2).map((cat) => Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(color: Colors.blue[50], borderRadius: BorderRadius.circular(8)),
                        child: Text(cat, style: const TextStyle(fontSize: 11, color: Color(0xFF1E64D3), fontWeight: FontWeight.bold)),
                      )).toList(),
                    )
                  ],
                ),
              )
            ],
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: () => Navigator.pushNamed(context, '/workerdetail', arguments: worker['id']),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF1E64D3),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                padding: const EdgeInsets.symmetric(vertical: 12)
              ),
              child: const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text("View Profile & Interview", style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
                  Icon(Icons.chevron_right, color: Colors.white),
                ],
              ),
            ),
          )
        ],
      ),
    );
  }
}