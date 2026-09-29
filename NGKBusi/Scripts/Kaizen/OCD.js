$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
    $("#selNIK").change(function () {
        selNIKChange();
    });

    function selNIKChange() {
        var currDivCode = $("#selNIK").find("option:selected").data("divid");
        var currPostCode = $("#selNIK").find("option:selected").data("postid");
        var currDiv = $("#selNIK").find("option:selected").data("divname");
        var currDept = $("#selNIK").find("option:selected").data("deptname");
        var currSection = $("#selNIK").find("option:selected").data("sectionname");
        var currSubSection = $("#selNIK").find("option:selected").data("subsectionname");
        currDiv = (currDiv) ? currDiv : "-";
        currDept = (currDept) ? currDept : "-";
        currSection = (currSection) ? currSection : "-";
        currSubSection = (currSubSection) ? currSubSection : "-";
        $("#hfDivision,#hfDept,#hfSection,#hfSubSection").val(null);
        $("#spanDivision,#spanDept,#spanSection,#spanSubSection").text("-");

        if ($("#selNIK").val() != "") {
            $("#hfDivision").val(currDiv);
            $("#spanDivision").text(currDiv);
            $("#hfDept").val(currDept);
            $("#spanDept").text(currDept);
            $("#hfSection").val(currSection);
            $("#spanSection").text(currSection);
            $("#hfSubSection").val(currSubSection);
            $("#spanSubSection").text(currSubSection);
        }

        $("#selLineLeader").val('').select2();

        if (currDivCode == "05" && (currPostCode == "I-A" || currPostCode == "0")) {
            $(".lineLeader").show("fade");
            $("#selLineLeader").attr("required", true);
        } else {
            $(".lineLeader").hide("fade");
            $("#selLineLeader").attr("required", false);
        }
    }

    $("#OCDModal").on('show.bs.modal', function () {
        if ($("#mainLoggedUser").data("id") == "PKL01") {
            $(".ocdRegNo").show();
            $("#txtRegNo").prop("required", true);
        } else {
            $(".ocdRegNo").hide();
            $("#txtRegNo").prop("required", false);
        }
    });

    $('#KaizenDataTable').on("click", "#btnAdd", function () {
        $("#formOCD")[0].reset();
        $("#selLineLeader").select2();
        $("#selNIK").select2();
        $("#spanDivision,#spanDept,#spanSection,#spanSubSection").text("-");
        $("#hfDivision,#hfDept,#hfSection,#hfSubSection,#hfOldRegNo,#hfCurrDataID").val(null);
        $(".lineLeader").hide();
        $("#selLineLeader").attr("required", false);
        $("#formOCD").attr("action", "/NGKBusi/Kaizen/OCD/Add");
        $("#txtDate").val($.datepicker.formatDate('dd-M-yy', new Date()));
        $("label.error").hide();
        $(".error").removeClass("error");
        $("#flScan").prop("required", true);
        if ($("#hfAllowNotScoring").val() == "true") {
            $("#formOCD .rbOCDScore,#formOCD #txtScore,#formOCD #txtReward").prop("required", false);
            $("#divScoreOCD,#btnDataOCDPrev,#btnDataOCDNext").fadeOut(function () {
                $("#divFormOCD,#btnDataSubmit").fadeIn();
            });
        } else {
            $("#formOCD .rbOCDScore,#formOCD #txtScore,#formOCD #txtReward").prop("required", true);
            $("#divScoreOCD,#btnDataOCDPrev,#btnDataSubmit").fadeOut(function () {
                $("#divFormOCD,#btnDataOCDNext").fadeIn();
            });
        }

        $("#OCDModal .modal-header h4").text("Add New Data");
    });

    //$("#txtDate").datepicker({
    //    dateFormat : "dd-M-yy"
    //});

    $("#selArea").change(function () {
        var currVal = $(this).val();
        if (currVal == "Lainnya") {
            $("#txtAreaOther").show("blind");
            $("#txtAreaOther").attr("required", true);
        } else {
            $("#txtAreaOther").hide("blind").val(null);
            $("#txtAreaOther").attr("required", false);
        }
    });

    $(".btnFeedback").click(function () {
        var currCTR = $(this);
        var currSpan = currCTR.closest("td").find(".spanHasFeedback");
        var currID = currCTR.data("id");
        var isActive = currCTR.data("active");
        if (isActive) {
            currCTR.LoadingOverlay("show");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/Kaizen/OCD/hasFeedback",
                data: { iID: currID },
                tryCount: 0,
                tryLimit: 3,
                success: function (data) {

                    currCTR.toggleClass("btn-warning btn-success");
                    currCTR.find("span").toggleClass("fa-exclamation fa-check");
                    currCTR.LoadingOverlay("hide");
                }, error: function (xhr, textStatus, errorThrown) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    currCTR.LoadingOverlay("hide");
                    alert("Error Occurred, Please Try Again.");
                }
            });
        }
    });

    $(".btnEdit").click(function () {
        var currCTR = $(this);
        var currID = currCTR.closest("tr").data("id");
        var regNo = currCTR.closest("tr").find("td:eq(0) a").text();
        var scope = currCTR.closest("tr").find("td:eq(1)").text();
        var nik = currCTR.closest("tr").find("td:eq(2)").text();
        var lineLeader = currCTR.closest("tr").data("lineleader");
        var dt = currCTR.closest("tr").find("td:eq(7)").text();
        var title = currCTR.closest("tr").find("td:eq(8)").text();
        var area = currCTR.closest("tr").find("td:eq(9)").text();
        $("#formOCD")[0].reset();
        $("#flScan,#formOCD .rbOCDScore,#formOCD #txtScore,#formOCD #txtReward").prop("required", false);
        $("#formOCD").attr("action", "/NGKBusi/Kaizen/OCD/Edit");
        $("#hfCurrDataID").val(currID);
        $("#txtRegNo,#hfOldRegNo").val(regNo);
        $("#selImpType").val(scope);
        $("#selNIK").val(nik).trigger('change');
        $("#selNIK").select2();
        //selNIKChange();

        $("#selLineLeader").val(lineLeader);
        $("#selLineLeader").select2();
        $("#txtDate").val(dt);
        $("#txtTitle").val(title);
        if ($("#selArea option[value='" + area + "']").length > 0) {
            $("#selArea").val(area);
            $("#selArea").change();
            $("#txtAreaOther").val("");
        } else {
            $("#selArea").val("Lainnya");
            $("#selArea").change();
            $("#txtAreaOther").val(area);
        }
        $("#divScoreOCD,#btnDataOCDPrev,#btnDataOCDNext").hide(function () {
            $("#divFormOCD,#btnDataSubmit").show();
        });
        $("#OCDModal .modal-header h4").text("Edit Data");

        $("#OCDModal").modal("show");
    });

    $(".btnDelete").click(function () {
        var currTR = $(this).closest("tr");
        var currID = currTR.data("id");
        var currRegNo = currTR.data("regno");
        if (confirm("Are you sure want to delete this data?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/Kaizen/OCD/Delete",
                data: { iID: currID, iRegNo: currRegNo },
                success: function (data) {
                    currRegNo
                    currTR.remove();
                    $("#OCDDataTable").trigger("update");
                }, error: function () {
                    $("#OCDDataTable").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });

    $("#btnDataOCDNext").click(function () {
        if ($('#flScan').is(':valid') && $('#selImpType').is(':valid') && $('#selNIK').is(':valid') &&
            $('#selLineLeader').is(':valid') && $('#txtTitle').is(':valid') && $('#selArea').is(':valid') && $('#txtAreaOther').is(':valid')) {

            $("#divFormOCD,#btnDataOCDNext").fadeOut(function () {
                $("#divScoreOCD,#btnDataOCDPrev,#btnDataSubmit").fadeIn();
            });
        } else {
            $("#btnDataSubmit").click();
        }
    });

    $("#btnDataOCDPrev").click(function () {
        $("#divScoreOCD,#btnDataOCDPrev,#btnDataSubmit").fadeOut(function () {
            $("#divFormOCD,#btnDataOCDNext").fadeIn();
        });
    });

    
});