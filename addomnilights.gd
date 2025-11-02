@tool
extends EditorScript

func _run():
	var root = get_editor_interface().get_edited_scene_root()
	if not root:
		push_error("No scene open!")
		return

	var added := 0
	var stack: Array = [root]
	while stack.size() > 0:
		var node = stack.pop_back()   # leave untyped
		if node is SpotLight3D and node.name == "office_light_spot":
				var omni := OmniLight3D.new()
				omni.name = "extra_omni"
				omni.light_energy = 100.0
				node.add_child(omni)
				omni.owner = root
				added += 1
		for child in node.get_children():
			stack.append(child)

	print("Added %d OmniLight3D nodes" % added)
