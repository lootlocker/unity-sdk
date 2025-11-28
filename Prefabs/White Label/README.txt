# LootLocker White Label Prefab

# Prerequisites
- LootLocker SDK installed and configured
- White Label Configured in the LootLocker Web Console
	- https://console.lootlocker.com/settings/platforms/white_label_login


# Installation
There are two ways of adding the Prefab to your game:
- By dragging the WhiteLabelLoginCanvasPrefab into your scene.
- By dragging the WhiteLabelLoginPrefab to a canvas in your scene

# Styling
The White Label Prefab can be styled by creating a new LootLocker Style Asset.
- Right Click in your Project view -> Create -> LootLocker -> Style Asset
	- Or duplicate one of the existing Style Assets located in this folder
- Add or replace the images and variables in the Style Asset to tweak it to your liking
- To Apply your changes, go to Window -> LootLocker -> Prefab -> Apply Style Asset
	- Select your preferred Style Asset and Click Apply Config

# Configurations
On the Prefab itself there are two settings that can be configured
- Use verification
	- This will change the message that the user gets when creating an account to tell them to check their email
		- Requires email validation to be set in the LootLocker Web Console
			- https://console.lootlocker.com/settings/platforms/white_label_login
- New Player Trigger Call
	- A trigger key that will be called when a new player is created
		- A trigger needs to be created in the LootLocker Web Console
			- https://console.lootlocker.com/triggers
