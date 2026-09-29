$(document).ready(function () {
    $("#btnSliderUpload").click(function () {
        $("#fileSliderImage").prop("required", true);
        $("#formSlider").attr("action", "/NGKBusi/Other/Slider/Add");
        $("#formSlider")[0].reset();
    });

    $(".btnSliderEdit").click(function () {
        var currID = $(this).data('id');
        var currPath = $(this).data('path');
        var currContent = $(this).data('content');
        var currExpired = $(this).data('expired');
        $("#fileSliderImage").prop("required", false);
        $("#formSlider").attr("action", "/NGKBusi/Other/Slider/Edit");
        $("#hfCurrID").val(currID);
        $("#hfCurrPath").val(currPath);
        tinymce.get('txtContent').setContent(currContent);
        $("#txtExpiredDate").val(currExpired);
        $("#sliderModal").modal();
    });

    $(".btnSliderDelete").click(function () {
        if (confirm("Are you sure want to delete this data ?")) {
            var currID = $(this).data("id");
            var currPath = $(this).data("path");
            var currImg = $(this).closest(".divSliderImg");
            $.ajax({
                type: "POST",
                url: '/NGKBusi/Other/Slider/Delete',
                data: {
                    dataID: currID,
                    path: currPath
                }, success: function () {
                    currImg.remove();
                }, error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $(".btnSliderVisible").click(function () {
        var currCTR = $(this);
        $.ajax({
            url: '/NGKBusi/Other/Slider/SetVisible',
            type: "post",
            data: {
                id: currCTR.data("id"),
                visible: currCTR.attr("data-visible")
            }, success: function () {
                if (currCTR.attr("data-visible") == 1) {
                    currCTR.removeClass("fas fa-eye").addClass("fas fa-eye-slash");
                } else {
                    currCTR.removeClass("fas fa-eye-slash").addClass("fas fa-eye");
                }
                currCTR.attr("data-visible", (currCTR.attr("data-visible") == 1 ? 0 : 1));
            }, error: function () {
                alert("Error occurred, please try again.");
            }
        });
    });
});