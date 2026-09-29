$(document).ready(function () {
    if ($("#tblLCAccess").length) {
        $("#tblLCAccess").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders'],
            widgetOptions: {
                filter_saveFilters: true
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }

    $(".btnLCA").click(function () {
        var currCTR = $(this);
        var currID = $(this).data("id");
        var currMenu = $(this).data("menu");
        if (confirm('Are you sure want to change this user access ?')) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/FA/LaborCost/SetAccess",
                data: {
                    iID: currID,
                    iMenu: currMenu
                },
                tryCount: 0,
                tryLimit: 3,
                success: function (data) {
                    if (currCTR.hasClass("btn-warning")) {
                        currCTR.removeClass("btn-warning").addClass("btn-success");
                        currCTR.find("i").removeClass("fa-times").addClass("fa-check");
                    } else {
                        currCTR.removeClass("btn-success").addClass("btn-warning");
                        currCTR.find("i").removeClass("fa-check").addClass("fa-times");
                    }
                },
                error: function (textStatus) {
                    if (textStatus == "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    alert("Error Occurred, Please Try Again.");
                }
            });
        }
    });
});