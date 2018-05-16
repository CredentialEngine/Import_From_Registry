/* Common functions for V2 site */

//Prevent IE console Error
if (typeof (console) == "undefined") {
	var console = { log: function () { return; } };
}
//

//Initialization
$(document).ready(function () {
	initHeaderMenus();
});
//

//Setup menus
function initHeaderMenus() {
	//Setup
	var menus = $("#mainSiteHeader .headerMenu");
	menus.each(function () {
		var menu = $(this);
		var toggler = menu.find(".headerMenuTitle");
		toggler.on("click", function () {
			menu.toggleClass("expanded").find(".headerMenuList").slideToggle(200);
			if (menu.hasClass("expanded")) {
				menus.not(menu).removeClass("expanded").find(".headerMenuList").slideUp(200);
			}
		})
	});

	//Close menus automatically
	$("html").not("#mainSiteHeader .headerMenu *").on("click", function () {
		menus.removeClass("expanded").find(".headerMenuList").slideUp(200);
	});
	menus.on("click", function (e) {
		e.stopPropagation();
	});

	//Mobile
	$("#btnMobileMenuOpen, #btnMobileMenuClose").on("click", function () {
		$("#mainSiteMenu").toggleClass("expanded");
	})
}
//


