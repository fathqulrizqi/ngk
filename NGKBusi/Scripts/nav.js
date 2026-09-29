$(document).ready(function () {

    //$(".sidebar-menu > ul > li > a").click(function () {
    //    $(".sidebar-submenu").not(".keep").slideUp(200);
    //});
    // Dropdown menu
    $(".sidebar-dropdown > a").click(function () {
        $(this).closest("ul").find(".sidebar-submenu").not(".keep").slideUp(200);
        if ($(this).parent().hasClass("active")) {
            $(".sidebar-dropdown").not(".keep").removeClass("active");
            $(this).parent().not(".keep").removeClass("active");
        } else {
            $(".sidebar-dropdown").not(".keep").removeClass("active");
            $(this).next(".sidebar-submenu").not(".keep").slideDown(200);
            $(this).parent().not(".keep").addClass("active");
        }
    });


    // close sidebar 
    $("#close-sidebar").click(function () {
        $(".page-wrapper").removeClass("toggled");
    });

    //show sidebar
    $("#show-sidebar").click(function () {
        $(".page-wrapper").addClass("toggled");
    });

    //switch between themes 
    var themes = "chiller-theme ice-theme cool-theme light-theme";
    $('[data-theme]').click(function () {
        $('[data-theme]').removeClass("selected");
        $(this).addClass("selected");
        $('.page-wrapper').removeClass(themes);
        $('.page-wrapper').addClass($(this).attr('data-theme'));
    });

    // switch between background images
    var bgs = "bg1 bg2 bg3 bg4";
    $('[data-bg]').click(function () {
        $('[data-bg]').removeClass("selected");
        $(this).addClass("selected");
        $('.page-wrapper').removeClass(bgs);
        $('.page-wrapper').addClass($(this).attr('data-bg'));
    });

    // toggle background image
    $("#toggle-bg").change(function (e) {
        e.preventDefault();
        $('.page-wrapper').toggleClass("sidebar-bg");
    });

    //custom scroll bar is only used on desktop
    //if (!/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
    //    $(".sidebar-content").mCustomScrollbar({
    //        axis: "y",
    //        autoHideScrollbar: true,
    //        scrollInertia: 300
    //    });
    //    $(".sidebar-content").addClass("desktop");

    //}
});