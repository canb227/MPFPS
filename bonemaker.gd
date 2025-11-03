@tool
extends EditorScript

func _run():
	var selection = get_editor_interface().get_selection()
	for node in selection.get_selected_nodes():
		if node is Skeleton3D:
			_generate_bone_attachments(node)

func _generate_bone_attachments(skel: Skeleton3D):
	for i in range(skel.get_bone_count()):
		var bone_name = skel.get_bone_name(i)
		var attach_name = bone_name + "_Attachment"

		# Skip if already exists
		if skel.has_node(attach_name):
			continue

		var attach = BoneAttachment3D.new()
		attach.name = attach_name
		attach.bone_name = bone_name
		skel.add_child(attach)
		attach.owner = skel.owner  # ensures it’s saved with the scene

		print("Created BoneAttachment3D for:", bone_name)
