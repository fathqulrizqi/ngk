$(document).ready(function () {
    $(".btnRewarded").click(function () {
        var currCTR = $(this);
        var currID = $(this).data("id");
        var imgLoad = '<i id="imgLoader" class="fa fa-spinner fa-pulse fa-3x fa-fw"></i>';
        currCTR.hide();
        currCTR.parent().append(imgLoad);
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Kaizen/Admin/setRewarded",
            data: {
                iID: currID
            },
            success: function (data) {
                if (currCTR.hasClass("btn-success")) {
                    currCTR.html('<span class="fa-stack"><i class="fa fa-money-bill fa-stack-1x"></i><i class="fa fa-ban fa-stack-2x text-danger"></i></span >');
                    currCTR.switchClass("btn-success", "btn-warning");
                    currCTR.parent().find(".spanRewarded").text("Belum");
                } else {
                    currCTR.html('<i class="fa fa-money-bill fa-2x"></i>');
                    currCTR.switchClass("btn-warning", "btn-success");
                    currCTR.parent().find(".spanRewarded").text("Sudah");
                }
                $(".tablesorter").trigger("update");
                $("#imgLoader").remove();
                currCTR.show();
            }, error: function () {
                $("#imgLoader").remove();
                currCTR.show();
            }
        });
    });
});