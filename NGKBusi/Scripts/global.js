$(document).ready(function () {
    //$("textarea").autogrow({
    //    onInitialize : true
    //});
    if ($("#globalSwal").length > 0) {
        Swal.fire($("#globalSwal").data("title"), $("#globalSwal").data("text"), $("#globalSwal").data("icon"));
    }

    if ($(".txtTinyMCE").length) {
        var input = $('<input/>')
            .attr('type', "file")
            .attr('name', "file")
            .attr('id', "flTinyMCE");
        //append the created file input
        $('.txtTinyMCE').append(input);
        tinymce.init({
            selector: '.txtTinyMCE',
            plugins: [
                'advlist autolink autoresize lists link image charmap print preview hr anchor pagebreak',
                'searchreplace wordcount visualblocks visualchars code fullscreen',
                'insertdatetime media nonbreaking save table contextmenu directionality',
                'emoticons template paste textcolor colorpicker textpattern imagetools codesample toc'
            ],
            toolbar1: 'undo redo | insert | styleselect | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | link image',
            toolbar2: 'print preview media | fontselect forecolor backcolor fontsizeselect emoticons | codesample',
            branding: false,
            image_advtab: true,
            paste_data_images: true,
            file_picker_callback: function (callback, value, meta) {
                if (meta.filetype === 'image') {
                    $('#flTinyMCE').trigger('click');
                    $('#flTinyMCE').on('change', function () {
                        var file = this.files[0];
                        var reader = new FileReader();
                        reader.onload = function (e) {
                            callback(e.target.result, {
                                alt: ''
                            });
                        };
                        reader.readAsDataURL(file);
                    });
                }
            }
        });

    }

    $(".imagezoom").click(function () {
        var img = $(this);
        $("#imagezoommodal").fadeIn();
        $("#imagezoomimg").attr("src", img.attr("src"));
    });

    $(".imagezoomclose").click(function () {
        $("#imagezoommodal").fadeOut();
    });

    $('.select2').each(function () {
        $(this).select2({ width: '100%' });
        $(".select2-container").css("width", "100%");
        $(".select2-selection__placeholder").css("color", "#555");
    });

    $('#headNav .dropdown').on('show.bs.dropdown', function (e) {
        $(this).find('.dropdown-menu').first().stop(true, true).slideDown(300);
    });

    $('#headNav .dropdown').on('hide.bs.dropdown', function (e) {
        $(this).find('.dropdown-menu').first().stop(true, true).slideUp(200);
    });

    var navChildWidth = 0;
    var navChildWidthMultiplier = 0;
    var navChildPadding = 0;
    $("#partialNav .navChild").each(function () {
        $(this).css("left", navChildWidth);
        navChildWidthMultiplier++;
        navChildPadding += parseInt($(this).css("padding-right").replace("px", ""), 10) * 2;
        navChildWidth = ($(this).width() * navChildWidthMultiplier) + navChildPadding;
    });


    $("#partialNav .navChild").click(function () {
        $(this).toggleClass("open");
    });
    $(".navChild").hover(function () {
        $(".navChild").not(this).find("div").toggleClass("black");
    });

    if ($(".partialNavMain").length > 0) {
        $("body").css("overflow-x", "hidden");
    }

    $(".dFrom").datepicker({ dateFormat: "dd-mm-yy", onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dTo").datepicker("option", "minDate", selDate); } });
    $(".dFromM").datepicker({ dateFormat: "dd-mm-yy", changeMonth: true, onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dTo").datepicker("option", "minDate", selDate); } });
    $(".dFromY").datepicker({ yearRange: "-100:+5", dateFormat: "dd-mm-yy", changeYear: true, onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dTo").datepicker("option", "minDate", selDate); } });
    $(".dFromMY").datepicker({ yearRange: "-100:+5", dateFormat: "dd-mm-yy", changeMonth: true, changeYear: true, onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dTo").datepicker("option", "minDate", selDate); } });
    $(".dTo").datepicker({ dateFormat: "dd-mm-yy", onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dFrom").datepicker("option", "maxDate", selDate); } });
    $(".dToM").datepicker({ dateFormat: "dd-mm-yy", changeMonth: true, onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dFrom").datepicker("option", "maxDate", selDate); } });
    $(".dToY").datepicker({ yearRange: "-100:+5", dateFormat: "dd-mm-yy", changeYear: true, onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dFrom").datepicker("option", "maxDate", selDate); } });
    $(".dToMY").datepicker({ yearRange: "-100:+5", dateFormat: "dd-mm-yy", changeMonth: true, changeYear: true, onSelect: function () { var selDate = $(this).datepicker("getDate"); $(".dFrom").datepicker("option", "maxDate", selDate); } });

    $(".jqDate").datepicker({ dateFormat: "dd-mm-yy" });
    $(".jqDateM").datepicker({ dateFormat: "dd-mm-yy", changeMonth: true });
    $(".jqDateY").datepicker({ yearRange: "-100:+5", dateFormat: "dd-mm-yy", changeYear: true });
    $(".jqDateMY").datepicker({ yearRange: "-100:+5", dateFormat: "dd-mm-yy", changeMonth: true, changeYear: true });
    $(".dFromM .ui-widget-header,.dFromY .ui-widget-header,.dFromMY .ui-widget-header" +
        ",.dToM .ui-widget-header,.dToY .ui-widget-header,.dToMY .ui-widget-header" +
        ",.jqDateM .ui-widget-header,.jqDateY .ui-widget-header,.jqDateMY .ui-widget-header").css("color", "#333 !important");

    $("select .ui-datepicker-year").css("color", "#333");
    $(".tablesorter-filter:disabled").hide();

    $(".jqTime").clockpicker({
        autoclose: true
    });


    $(".animsition-link").each(function (e) {
        if (window.location.pathname.indexOf($(this).attr("href")) >= 0) {
            $(this).css("font-weight", "bold").css("color", "white");

            var currSideDropdown = $(this).parent().closest('.sidebar-dropdown:not(.favoriteMenu)');
            while (currSideDropdown.length > 0) {
                currSideDropdown.addClass("active");
                currSideDropdown = currSideDropdown.parent().closest('.sidebar-dropdown:not(.favoriteMenu)');
                if (currSideDropdown.length < 0) {
                    break;
                }
            }

            var currSideSubmenu = $(this).parent().closest('.sidebar-submenu:not(.favoriteMenu)');
            while (currSideSubmenu.length > 0) {
                currSideSubmenu.css("display", "block");
                currSideSubmenu = currSideSubmenu.parent().closest('.sidebar-submenu:not(.favoriteMenu)');
                if (currSideSubmenu.length < 0) {
                    break;
                }
            }
        }
    });
});
function setRate(currRate, currRateGroup) {
    currRateGroup = currRateGroup;
    currRate = currRate;
    $(currRateGroup).find(".rateStar").each(function () {
        $(this).attr("src", "/NGKBusi/Content/Images/grey-star.png");
        var ctrRate = $(this).data("rate");
        if (ctrRate <= currRate) {
            $(this).attr("src", "/NGKBusi/Content/Images/gold-star.png");
        }
    });
}
var delay = (function () {
    var timer = 0;
    return function (callback, ms) {
        clearTimeout(timer);
        timer = setTimeout(callback, ms);
    };
})();

function printElement(element) {
    var divToPrint = element;

    var newWin = window.open('', 'Print-Window');

    newWin.document.open();

    newWin.document.write('<!DOCTYPE html><html><body onload="window.print()">' + divToPrint.html() + '</body></html>');

    newWin.document.close();

    setTimeout(function () { newWin.close(); }, 10);

}




// update by Ihsan

function breakNumbers(Numbers) {
    var nominalValue = Numbers.toLocaleString("id-ID");
    return nominalValue;
}

$(document).on('click', '.addFavorite', function (e) {
    e.preventDefault();
    var idMenu = $(this).attr("data-id");
    var icon = $(this).children('i');
    var urlString = "/NGKbusi/Home/AddFavoriteMenu";
    $.ajax({
        url: urlString,
        type: "POST",
        cache: false,
        dataType: 'json',
        data: { id: idMenu },
        success: function (response) {
            if (response.status == 1) {
                icon.removeAttr("style");
                icon.css({ "display": "block" });
            }
        }
    });
});

$(document).on('click', '.removeFavorite', function (e) {
    e.preventDefault();
    var idMenu = $(this).attr("data-id");
    var icon = $(this).children('i');
    var urlString = "/NGKbusi/Home/RemoveFavoriteMenu";
    $.ajax({
        url: urlString,
        type: "POST",
        cache: false,
        dataType: 'json',
        data: { id: idMenu },
        success: function (response) {
            if (response.status == 1) {
                icon.removeAttr("style");
                icon.css({ "display": "inline", "color": "black", "background-color": "unset" });
            }
        }
    });
});

$(document).on('click', '.removeFromFavoriteList', function (e) {
    e.preventDefault();
    var idMenu = $(this).attr("data-id");
    var a = $(this).parent();
    var urlString = "/NGKbusi/Home/RemoveFavoriteMenu";
    $.ajax({
        url: urlString,
        type: "POST",
        cache: false,
        dataType: 'json',
        data: { id: idMenu },
        success: function (response) {
            if (response.status == 1) {
                a.remove();
            }
        }
    });
});