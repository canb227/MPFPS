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
			# Add spotlight if missing
			if not node.has_node("office_light_spot"):
				var spot := SpotLight3D.new()
				spot.name = "office_light_spot"
				spot.light_energy = 10.0
				spot.light_indirect_energy = 5.0
				spot.light_specular = 0.3
				spot.spot_range = 8.0
				spot.spot_angle = 80.0
				spot.spot_angle_attenuation = 1.27
				spot.rotation_degrees = Vector3(-90, 0, 0)
				node.add_child(spot)
				spot.owner = root
				added += 1

			# Add omni light if missing
			if not node.has_node("office_light_omni"):
				var omni := OmniLight3D.new()
				omni.name = "office_light_omni"
				omni.light_energy = 1.4
				omni.light_indirect_energy = 2.75
				omni.omni_range = 2.5
				node.add_child(omni)
				omni.owner = root
				added += 1

		for child in node.get_children():
			stack.append(child)

	print("Added %d lights" % added)
