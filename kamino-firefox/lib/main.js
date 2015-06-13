var { ToggleButton } = require('sdk/ui/button/toggle');
var panels = require("sdk/panel");
var self = require("sdk/self");

var button = ToggleButton({
	id: "kamino-button",
	label: "Kamino Web Manager",
	icon: {
		"16": "./kamino-16.png",
		"32": "./kamino-32.png",
		"64": "./kamino-64.png"
	},
	onChange: handleChange
});

var panel = panels.Panel({
	contentURL : self.data.url("panel.html"),
	onHide : handleHide,
	contentScriptFile : self.data.url("panel.js")
});

function handleChange(state) {
	if (state.checked) {
		panel.show({ position: button });
	}
}

function handleHide() {
  button.state('window', {checked: false});
}

panel.port.on("panel-click", function(sourceId) {
	console.log("Panel click on: " + sourceId);
});
