$(document).ready(function () {
    $(".btnImplemented").on("click", function () {
        var ctr = $(this);
        var currID = ctr.attr("data-id");
        $('#formPI')[0].reset();
        $('#formPI .select2').val(null).trigger('change');
        $("#hfPIImplementID").val(currID);
        var implementStatus = ctr.hasClass("btn-success") ? 1 : 0;
        $(".rbPIImplemented[value='" + implementStatus + "']").prop("checked", true);

        if (implementStatus > 0) {
            $("#divImplementWrap").show();
        } else {
            $("#divImplementWrap").hide();
        }
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Kaizen/PI/getImplement",
            data: {
                iID: currID
            },
            success: function (data) {
                var activeEditor = tinyMCE.get('txtPIImplement');
                var getNIK = [];
                $.each(data, function (i, v) {
                    activeEditor.setContent(v.content);
                    $.each(v.implementor, function (x, z) {
                        getNIK.push(z.userNIK);
                    });
                });
                $("#selPIImplementor").val(getNIK).trigger("change");
                $("#txtPIImplement").focus();

            }, error: function () {
                alert("Error occurred, please try again !");
            }
        });
    });
    $(".rbPIImplemented").change(function () {
        var ctr = $(this);
        if (ctr.val() > 0) {
            $("#divImplementWrap").fadeIn();
        } else {
            $("#divImplementWrap").fadeOut();
        }
    });
});