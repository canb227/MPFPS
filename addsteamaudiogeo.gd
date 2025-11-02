# res://addons/add_steam_audio_geometry.gd
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
		if node is CollisionShape3D and node.shape is BoxShape3D:
			# Only add if it doesn't already have one
			if not node.has_node("SteamAudioGeometry"):
				var geo := SteamAudioGeometry.new()
				geo.name = "SteamAudioGeometry"
				node.add_child(geo)
				geo.owner = root   # make sure it’s saved with the scene
				added += 1
		for child in node.get_children():
			stack.append(child)

	print("Added %d SteamAudioGeometry nodes" % added)
