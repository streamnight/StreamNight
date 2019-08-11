(function (window, document) {
var menu = document.getElementById('menu'),
  WINDOW_CHANGE_EVENT = ('onorientationchange' in window) ? 'orientationchange':'resize';
  
function closeMenu() {
	menu.classList.remove('menu-open');
}

document.getElementById('toggle').addEventListener('click', function (e) {
  menu.classList.toggle('menu-open')
  e.preventDefault();
});

window.addEventListener(WINDOW_CHANGE_EVENT, closeMenu);
})(this, this.document);


var navLinks = document.getElementById("menu").querySelectorAll("ul > li > a");

navLinks.forEach(function (link) {
    if (link.pathname == location.pathname) {
        link.parentElement.classList.add("current");
    }
});