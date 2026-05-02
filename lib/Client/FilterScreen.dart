import 'package:flutter/material.dart';

class Category {
  final String categoryName;
  final List<String> skills;

  Category({required this.categoryName, required this.skills});
}

class FilterScreen extends StatefulWidget {
  const FilterScreen({super.key});

  @override
  State<FilterScreen> createState() => _FilterScreenState();
}

class _FilterScreenState extends State<FilterScreen> {

  final List<String> cities = [
    'Islamabad','Rawalpindi','Lahore','Karachi',
    'Faisalabad','Peshawar','Multan','Quetta'
  ];

  final List<Category> categories = [
    Category(categoryName: 'Cleaning', skills: ['Deep Cleaning','Dusting','Laundry']),
    Category(categoryName: 'Cooking', skills: ['Chinese','Italian','Desi']),
    Category(categoryName: 'Driving', skills: ['Manual','Auto','Bike']),
  ];

  String gender = '';
  String city = '';
  List<String> selectedCategories = [];
  Map<String, List<String>> subSkills = {};

  @override
  void initState() {
    super.initState();
    for (var cat in categories) {
      subSkills[cat.categoryName] = [];
    }
  }

  void toggleCategory(String name) {
    setState(() {
      if (selectedCategories.contains(name)) {
        selectedCategories.remove(name);
      } else {
        selectedCategories.add(name);
      }
    });
  }

  void toggleSubSkill(String cat, String skill) {
    setState(() {
      if (subSkills[cat]!.contains(skill)) {
        subSkills[cat]!.remove(skill);
      } else {
        subSkills[cat]!.add(skill);
      }
    });
  }

  void reset() {
    setState(() {
      gender = '';
      city = '';
      selectedCategories.clear();
      subSkills.forEach((key, value) => value.clear());
    });
  }

  void apply() {
    Navigator.pop(context, {
      "gender": gender,
      "city": city,
      "categories": selectedCategories,
      "subSkills": subSkills
    });
  }

  Widget chip(String text, bool active, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.all(5),
        padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 8),
        decoration: BoxDecoration(
          color: active ? const Color(0xFF1E64D3) : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: Colors.grey.shade300),
        ),
        child: Text(
          text,
          style: TextStyle(
            color: active ? Colors.white : Colors.black,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
    );
  }

  Widget section(String title, Widget child) {
    return Container(
      margin: const EdgeInsets.only(bottom: 15),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: Colors.grey.shade200),
        boxShadow: [
          BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 5)
        ],
        color: Colors.white,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: const TextStyle(
                  fontWeight: FontWeight.bold, fontSize: 15)),
          const SizedBox(height: 10),
          child
        ],
      ),
    );
  }

  void showCityPicker() {
    showModalBottomSheet(
      context: context,
      builder: (_) {
        return ListView(
          children: cities.map((c) {
            return ListTile(
              title: Text(c),
              trailing: city == c ? const Icon(Icons.check) : null,
              onTap: () {
                setState(() => city = c);
                Navigator.pop(context);
              },
            );
          }).toList(),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,

      appBar: AppBar(
        title: const Text("FILTERATION"),
        centerTitle: true,
      ),

      body: Stack(
        children: [

          /// MAIN CONTENT
          SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(15, 10, 15, 90),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [

                /// TAGS
                Wrap(
                  spacing: 5,
                  children: [
                    if (gender.isNotEmpty)
                      Chip(
                        label: Text(gender),
                        onDeleted: () => setState(() => gender = ''),
                      ),

                    if (city.isNotEmpty)
                      Chip(
                        label: Text(city),
                        onDeleted: () => setState(() => city = ''),
                      ),

                    ...selectedCategories.map((e) => Chip(
                      label: Text(e),
                      onDeleted: () => toggleCategory(e),
                    ))
                  ],
                ),

                /// GENDER
                section(
                  "GENDER",
                  Row(
                    children: ['Male','Female','Both'].map((g) {
                      return Expanded(
                        child: chip(
                          g,
                          gender == g,
                          () => setState(() => gender = g),
                        ),
                      );
                    }).toList(),
                  ),
                ),

                /// SKILLS
                section(
                  "SKILLS",
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: categories.map((c) {
                        return chip(
                          c.categoryName,
                          selectedCategories.contains(c.categoryName),
                          () => toggleCategory(c.categoryName),
                        );
                      }).toList(),
                    ),
                  ),
                ),

                /// CITY
                section(
                  "CITY",
                  GestureDetector(
                    onTap: showCityPicker,
                    child: Container(
                      padding: const EdgeInsets.all(15),
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: Colors.grey),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(city.isEmpty ? "Select City" : city),
                          const Icon(Icons.arrow_drop_down)
                        ],
                      ),
                    ),
                  ),
                ),

                /// SUB SKILLS
                if (selectedCategories.isNotEmpty)
                  section(
                    "SUB CATEGORY",
                    Column(
                      children: categories
                          .where((c) => selectedCategories.contains(c.categoryName))
                          .map((cat) {
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(cat.categoryName,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                            Wrap(
                              children: cat.skills.map((s) {
                                return chip(
                                  s,
                                  subSkills[cat.categoryName]!.contains(s),
                                  () => toggleSubSkill(cat.categoryName, s),
                                );
                              }).toList(),
                            ),
                            const SizedBox(height: 10),
                          ],
                        );
                      }).toList(),
                    ),
                  ),
              ],
            ),
          ),

          /// FOOTER
          Positioned(
            bottom: 0,
            left: 0,
            right: 0,
            child: Container(
              padding: const EdgeInsets.all(15),
              decoration: const BoxDecoration(
                color: Colors.white,
                border: Border(top: BorderSide(color: Colors.grey)),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: ElevatedButton(
                      onPressed: reset,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.grey,
                      ),
                      child: const Text("Reset"),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: apply,
                      child: const Text("Apply"),
                    ),
                  ),
                ],
              ),
            ),
          )
        ],
      ),
    );
  }
}