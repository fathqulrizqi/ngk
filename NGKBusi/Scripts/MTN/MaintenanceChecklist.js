$(document).ready(function () {
    var currPeriod = $("#hfPeriod").val();
    var currPeriodFY = $("#hfPeriodFY").val();
    var currArea = $("#hfArea").val();
    var currMachineName = $("#hfMachineName").val();
    var currMachineNo = $("#hfMachineNo").val();
    $(".txtMCDate").datepicker();
    $(".txtMCDate").each(function () {
        var currCtr = $(this);
        var currInterval = $(this).data("interval");
        if (currCtr.val().length > 0) {
            $("td[data-interval='" + currInterval + "'].available").removeClass("disabled");
        } else {
            $("td[data-interval='" + currInterval + "'].available").addClass("disabled");
        }
    });
    $(".txtMCDate").change(function () {
        var currCtr = $(this);
        var currInterval = $(this).data("interval");
        currCtr.LoadingOverlay("show");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/setMChecklistHeaderDate",
            method: "POST",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iPeriod: currPeriod,
                iPeriodFY: currPeriodFY,
                iArea: currArea,
                iMachineName: currMachineName,
                iMachineNo: currMachineNo,
                iDate: currCtr.val(),
                iInterval: currCtr.data("interval")
            }, success: function () {
                if (currCtr.val().length > 0) {
                    $("td[data-interval='" + currInterval + "'].available").removeClass("disabled");
                    $(".txtNote[data-interval='" + currInterval + "']").prop("disabled", false);
                } else {
                    $("td[data-interval='" + currInterval + "'].available").addClass("disabled");
                    $(".txtNote[data-interval='" + currInterval + "']").prop("disabled", true);
                }
                currCtr.LoadingOverlay("hide");
            }, error: function () {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                currCtr.LoadingOverlay("show");
                currCtr.val('').change();
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    $(".txtNote").change(function () {
        var currCtr = $(this);
        var currInterval = $(this).data("interval");
        currCtr.LoadingOverlay("show");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/setMChecklistHeaderNote",
            method: "POST",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iPeriod: currPeriod,
                iPeriodFY: currPeriodFY,
                iArea: currArea,
                iMachineName: currMachineName,
                iMachineNo: currMachineNo,
                iNote: currCtr.val(),
                iInterval: currCtr.data("interval")
            }, success: function () {
                currCtr.LoadingOverlay("hide");
            }, error: function () {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                currCtr.LoadingOverlay("show");
                currCtr.val('').change();
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    var currChecklistCTR = "";
    var currChecklistTR = "";
    //$(document).on("click", ".tdChecklistDataCondition.available:not(.disabled),.tdChecklistDataAction.available:not(.disabled)",function () {
    $(document).on("click", ".tdChecklistDataAction.available:not(.disabled)",function () {
        var currCtr = $(this);
        var isAllowEdit = currCtr.data("allowedit");
        if (isAllowEdit == "False") {
            alert("Data already signed, please return first!");

            return false;
        }
        var currTR = currCtr.closest("tr");
        var headerID = $("#hfHeaderID").val();
        var itemID = currTR.data("itemid");
        var cat = currCtr.data("category");
        var interval = currCtr.data("interval");
        var module = currCtr.data("module");
        var imageNote = $("#txtImageNote").val();
        currChecklistCTR = currCtr;
        currChecklistTR = currTR;

        $("#hfFileHeaderID").val(headerID);
        $("#hfCategory").val(cat);
        $("#hfModule").val(module);
        $("#hfInterval").val(interval);
        $("#hfItemID").val(itemID);
        $("#fileUploadModal").modal();
    });
    $("#formChecklistReport").submit(function (e) {
        e.preventDefault();
        var checkList = '<i class="fa fa-check" style="color:green"></i>';
        var interval = $("#hfInterval").val();
        var module = $("#hfModule").val();
        var cat = $("#hfCategory").val();
        $("#fileUploadModal .modal-content").LoadingOverlay("show");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/setMChecklistLines",
            method: "POST",
            tryCount: 0,
            tryLimit: 3,
            data: new FormData(this),
            cache: false,
            contentType: false,
            processData: false,
            success: function (data) {
                currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "']").empty();
                currChecklistTR.find(".tdChecklistDataAction[data-interval='" + interval + "'][data-category='" + cat + "']").empty();
                currChecklistCTR.append(checkList);
               // $("#fileUploadModal").modal('hide');      
                switch (module) {
                    case "ConditionOK":
                        currChecklistTR.find(".tdChecklistDataAction[data-interval='" + interval + "'][data-category='" + cat + "']").empty();
                        currChecklistTR.find(".tdChecklistDataAction[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ActionOK']").text("OK");
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionNOT']").empty();
                        break;
                    case "ConditionNOT":
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionOK']").empty();
                        currChecklistTR.find(".tdChecklistDataAction[data-interval='" + interval + "'][data-category='" + cat + "']").empty();
                        break;
                    case "ActionOK":
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionNOT']").empty();
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionOK']").append(checkList);
                        currChecklistCTR.empty().text("OK");
                        break;
                    case "ActionRepair":
                    case "ActionChange":
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionOK']").empty();
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionNOT']").append(checkList);
                        currChecklistTR.find(".tdChecklistDataCondition[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ConditionOK']").empty();
                        break;
                }
                $("#formChecklistReport")[0].reset();
                $("#fileUploadModal").modal('hide');
                $("#fileUploadModal .modal-content").LoadingOverlay("hide");
            }, error: function () {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#fileUploadModal").modal('hide');
                $("#fileUploadModal .modal-content").LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });

    $.ajax({
        url: "/NGKBusi/MTN/MachineDatabase/getMChecklistLines",
        method: "GET",
        tryCount: 0,
        tryLimit: 3,
        data: {
            iHeaderID: $("#hfHeaderID").val()
        }, success: function (data) {
            $(".tdChecklistDataCondition.available:not(.disabled),.tdChecklistDataAction.available:not(.disabled)").each(function () {
                var currCtr = $(this);
                var currTR = currCtr.closest("tr");
                var headerID = $("#hfHeaderID").val();
                var itemID = currTR.data("itemid");
                var cat = currCtr.data("category");
                var interval = currCtr.data("interval");
                var module = currCtr.data("module");
                var checkList = '<i class="fa fa-check" style="color:green"></i>';
                var currData = data.filter(function (e) {
                    return e.Item_ID === itemID && e.Category === cat && e.Interval === interval;
                });
                switch (module) {
                    case "ConditionOK":
                        if (currData.length && currData[0].isCondition === true) {
                            currCtr.append(checkList);
                            currTR.find(".tdChecklistDataAction[data-interval='" + interval + "'][data-category='" + cat + "'][data-module='ActionOK']").text("OK");
                        }
                        break;
                    case "ConditionNOT":
                        if (currData.length && currData[0].isCondition === false) {
                            currCtr.append(checkList);
                        }
                        break;
                    case "ActionOK":
                        if (currData.length && currData[0].isCondition === true) {
                            currCtr.text("OK");
                        }
                        break;
                    case "ActionRepair":
                        if (currData.length && currData[0].isAction === true) {
                            currCtr.append(checkList);
                        }
                        break;
                    case "ActionChange":
                        if (currData.length && currData[0].isAction === false) {
                            currCtr.append(checkList);
                        }
                        break;
                }
            });
        }, error: function () {
            if (textStatus === "timeout") {
                this.tryCount++;
                if (this.tryCount <= this.tryLimit) {
                    $.ajax(this);
                    return;
                }
            }
        }
    });
});