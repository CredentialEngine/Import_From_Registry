//Run this is in the parent window (the one that has the iframe in it)
//Does not require jQuery
if (window.addEventListener) {
    window.addEventListener("load", function () { window.addEventListener("message", resizeFrame) });
}
else if (window.attachEvent) {
    window.attachEvent("onload", function () { window.attachEvent("message", resizeFrame) });
}

function resizeFrame(message) {
    if (message.data.action && message.data.action === "resizeFrame") {
        var frame = Array.prototype.slice.call(document.getElementsByTagName("iframe")).filter(function (m) { return m.contentWindow === message.source; })[0];
        var styles = window.getComputedStyle(frame);
        var setHeight = message.data.height + parseFloat(styles.getPropertyValue("border-top-width")) + parseFloat(styles.getPropertyValue("border-bottom-width"));
        frame.height = setHeight;
        frame.style.height = setHeight + "px";
    }
}
//