window.addEventListener('click', function(event) {
	var t = event.target;
	if (t.nodeName == 'A') {
		self.port.emit('panel-click', t.id);
	}
}, false);
