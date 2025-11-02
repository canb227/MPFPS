@tool
extends EditorScript

func _run():
	var root = get_editor_interface().get_edited_scene_root()
	if not root:
		push_error("No scene open!")
		return

	var added := 0
	var stack := [root]
	while stack.size() > 0:
		var node = stack.pop_back()
		if node is Node3D and node.name.begins_with("OfficeLight_"):
			if not node.has_node("office_light_spot"):
				var spot := SpotLight3D.new()
				spot.name = "office_light_spot"
				spot.light_energy = 200.0
				spot.spot_angle = 45.0
				# Rotate -90° around X
				spot.rotation_degrees = Vector3(-90, 0, 0)
				node.add_child(spot)
				spot.owner = root   # ensure it’s saved with the scene
				added += 1
		for child in node.get_children():
			stack.append(child)

	print("Added %d spotlights" % added)
