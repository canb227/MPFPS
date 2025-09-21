extends Node
class_name GDSteamNetwork
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

func HostServer() -> void:
	var peer = SteamMultiplayerPeer.new()
	var error = peer.create_host(0)
	if error:
		print(error)
	multiplayer.multiplayer_peer = peer
	
	
func JoinServer(id:int) -> void:
	var peer = SteamMultiplayerPeer.new()
	var error = peer.create_client(id,0)
	if error:
		print(error)
	multiplayer.multiplayer_peer = peer
	
