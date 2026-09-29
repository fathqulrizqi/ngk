$(document).ready(function () {
    if ($("#tblMSData").length) {
        $("#tblMSData").tablesorter({
            widgets: ['stickyHeaders']
        });
    }
    if ($("#tblMSMonthlyData").length) {
        $("#tblMSMonthlyData").tablesorter({
            widgets: ['stickyHeaders']
        });
    }

    $("#tblMSData tbody td:not(:nth-child(1)):not(:nth-child(2))").click(function () {
        if ($("#hfIsLock").val() == "Locked") {
            alert("This schedule is LOCKED, please UNLOCK/RETURN to edit!");
            return false;
        }
        var currTD = $(this);
        var currTR = currTD.closest("tr");
        var areaID = currTR.data("areaid");
        var area = currTR.data("area");
        var machineID = currTR.data("machineid");
        var machineNo = currTR.data("machineno");
        var machine = currTR.data("machinename");
        var currYear = currTD.data("year");
        var currFY = currTR.data("fy");
        var currMonth = currTD.data("month");
        var currSchedule = currTD.data("schedule");
        currTD.LoadingOverlay("show");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/setMaintenanceSchedule",
            method: "POST",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iAreaID: areaID,
                iArea: area,
                iMachineID: machineID,
                iMachineNo: machineNo,
                iMachine: machine,
                iYear: currYear,
                iFY: currFY,
                iMonth: currMonth,
                iSchedule: currSchedule
            }, success: function (data) {
                currTD.LoadingOverlay("hide");
                if (data) {
                    currTD.css("background-color", "#F39814");
                    currTD.text(currSchedule);
                } else {
                    currTD.text("");
                    currTD.css("background-color", "white");
                }
            }, error: function () {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                currTD.LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    $("#tblMSData tbody tr").each(function (i, v) {
        var currTR = $(this);
        var areaID = currTR.data("areaid");
        var area = currTR.data("area");
        var machineID = currTR.data("machineid");
        var machineNo = currTR.data("machineno");
        var machine = currTR.data("machinename");
        var currYear = currTR.data("year");
        var currFY = currTR.data("fy");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/getMaintenanceSchedule",
            method: "GET",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iAreaID: areaID,
                iArea: area,
                iMachineID: machineID,
                iMachineNo: machineNo,
                iMachine: machine,
                iYear: currYear,
                iFY: currFY,
            }, success: function (data) {
                var iInterval = 1;
                var checkInput = false;
                $(".tdScheduleData", currTR).each(function (e, v) {
                    var currYear = $(this).data("year");
                    var currMonth = $(this).data("month");
                    var currSchedule = $(this).data("schedule");
                    var currFY = $(this).data("fy");
                    var stat;
                    if (data.schedules.length > 0) {
                        stat = data.schedules.filter(function (e) {
                            return e.Month === currMonth && e.Year === currYear && e.Period_FY === currFY && e.Machine_Name.trim() === machine.trim() && e.Area === area && e["is" + currSchedule] === true;
                        });
                        if (stat.length) {
                            if (checkInput === true) {
                                iInterval++;
                            }
                            checkInput = false;
                            headersStatus = data.headers.filter(function (e) {
                                return e.Period === currYear && e.Period_FY === currFY && e.Machine_Name.trim() === machine.trim() && e.Area === area && e["Date" + iInterval] !== null;
                            });
                            $(this).css("background-color", "#F39814");
                            if (headersStatus.length) {
                                $(this).css("background-color", "green");
                            }
                            $(this).text(currSchedule);
                        } else {
                            checkInput = true;
                            $(this).text("");
                            $(this).css("background-color", "white");
                        }
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
                currTD.LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    var currTD;
    $('[data-toggle="popover"]').click(function () {
        if ($("#hfIsLock").val() == "Locked") {
            alert("This schedule is LOCKED, please UNLOCK/RETURN to edit!");
            return false;
        }
        currTD = $(this);
    });
    $(document).on("click", ".popoverButton button", function () {
        if ($("#hfIsLock").val() == "Locked") {
            alert("This schedule is LOCKED, please UNLOCK/RETURN to edit!");
            return false;
        }
        var currCategory = $(this).text();
        var currTR = currTD.closest("tr");
        var areaID = currTR.data("areaid");
        var area = currTR.data("area");
        var machineID = currTR.data("machineid");
        var machineNo = currTR.data("machineno");
        var machine = currTR.data("machinename");
        var currYear = currTR.data("year");
        var currFY = currTR.data("fy");
        var currMonth = currTR.data("month");
        var currDay = currTD.data("day");
        currTD.LoadingOverlay("show");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/setMaintenanceScheduleMonthly",
            method: "POST",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iAreaID: areaID,
                iArea: area,
                iMachineID: machineID,
                iMachineNo: machineNo,
                iMachine: machine,
                iFY: currFY,
                iYear: currYear,
                iMonth: currMonth,
                iDay: currDay,
                iCategory: currCategory
            }, success: function (data) {
                currTD.LoadingOverlay("hide");
                currTD.text(currCategory);
                switch (currCategory) {
                    case "3B":
                        currTD.css("background-color", "#ffc107");
                        break;
                    case "6B":
                        currTD.css("background-color", "#28a745");
                        break;
                    case "1T":
                        currTD.css("background-color", "#17a2b8");
                        break;
                    default:
                        currTD.text("");
                        currTD.css("background-color", "white");
                        break;
                }
            }, error: function () {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                currTD.LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });

    });
    var popOverContent = '<div id="btn0" class="btn-group btn-group-sm popoverButton">' +
        '<button type="button" class="btn btn-warning" style="color:white;">3B</button>' +
        '<button type="button" class="btn btn-success">6B</button>' +
        '<button type="button" class="btn btn-info">1T</button>' +
        '<button type="button" class="btn btn-light">X</button>' +
        '</div>';

    if ($("#hfIsLock").val() != "Locked") {
        $('[data-toggle="popover"]').popover({
            trigger: 'focus',
            html: true,
            sanitize: false,
            content: popOverContent
        });
    }

    $("#tblMSMonthlyData tbody tr").each(function (i, v) {
        var currTR = $(this);
        var areaID = currTR.data("areaid");
        var area = currTR.data("area");
        var machineID = currTR.data("machineid");
        var machineNo = currTR.data("machineno");
        var machine = currTR.data("machinename");
        var currYear = currTR.data("year");
        var currMonth = currTR.data("month");
        var currFY= currTR.data("fy");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/getMaintenanceScheduleMonthly",
            method: "GET",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iAreaID: areaID,
                iArea: area,
                iMachineID: machineID,
                iMachineNo: machineNo,
                iMachine: machine,
                iYear: currYear,
                iFY: currFY,
                iMonth: currMonth
            }, success: function (data) {
                $(".tdScheduleMonthDataNote", currTR).each(function (e, v) {
                    $(this).text(data.scheduleYearly.Note);
                });
                $(".tdScheduleMonthData", currTR).each(function (e, v) {
                    var currDay = $(this).data("day");
                    var stat;
                    if (data.schedules.length > 0) {
                        stat = data.schedules.filter(function (e) {
                            return e.Day === currDay;
                        });
                        if (stat.length) {
                            $(this).text(stat[0].Category);
                            switch (stat[0].Category) {
                                case "3B":
                                    $(this).css("background-color", "#ffc107");
                                    break;
                                case "6B":
                                    $(this).css("background-color", "#28a745");
                                    break;
                                case "1T":
                                    $(this).css("background-color", "#17a2b8");
                                    break;
                                default:
                                    currTD.text("");
                                    $(this).css("background-color", "white");
                                    break;
                            }
                        } else {
                            $(this).text("");
                            $(this).css("background-color", "white");
                        }
                    }
                });
                $(".tdScheduleMonthDataActual", currTR).each(function (e, v) {
                    var currDay = $(this).data("day");
                    var currDate = new Date(currYear, currMonth - 1, currDay);
                    var stat;
                    if (data.headers.length > 0) {
                        headersStatus = data.headers.filter(function (e) {
                            return e.Period_FY == currFY && e.Machine_Name.trim() === machine.trim() && e.Area === area;
                            //return e.Period === currYear && e.Period_FY == currFY && e.Machine_Name.trim() === machine.trim() && e.Area === area;
                        });

                        if (headersStatus.length) {
                            var milli1 = headersStatus[0].Date1 ?.replace(/\/Date\((-?\d+)\)\//, '$1');
                            var d1 = new Date(parseInt(milli1));
                            var milli2 = headersStatus[0].Date2 ?.replace(/\/Date\((-?\d+)\)\//, '$1');
                            var d2 = new Date(parseInt(milli2));
                            var milli3 = headersStatus[0].Date3 ?.replace(/\/Date\((-?\d+)\)\//, '$1');
                            var d3 = new Date(parseInt(milli3));
                            var milli4 = headersStatus[0].Date4 ?.replace(/\/Date\((-?\d+)\)\//, '$1');
                            var d4 = new Date(parseInt(milli4));

                            //console.log(machine + " || " + headersStatus[0].Date4 + " || " + d4.toDateString() + " || " + currDate.toDateString());
                            if (d1.toDateString() == currDate.toDateString()) {
                                $(this).text("3B");
                                $(this).css("background-color", "#ffc107");
                            } else if (d2.toDateString() === currDate.toDateString()) {
                                $(this).text("6B");
                                $(this).css("background-color", "#28a745");
                            } else if (d3.toDateString() === currDate.toDateString()) {
                                $(this).text("3B");
                                $(this).css("background-color", "#ffc107");
                            } else if (d4.toDateString() === currDate.toDateString()) {
                                $(this).text("1T");
                                $(this).css("background-color", "#17a2b8");
                            }
                        } else {
                            $(this).text("");
                            $(this).css("background-color", "white");
                        }
                    }
                });
            }, error: function (textStatus) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                //currTD.LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });

    $("#selMSPeriod").change(function () {
        var y = $(this).find(":selected").data("year");
        $("#hfPeriod").val(y);
        $("#formMaintenanceSchedule").submit();
    });

    var currNotesTR = "";
    $(".tdScheduleMonthDataNote").click(function () {
        if ($("#hfIsLock").val() == "Locked") {
            alert("This schedule is LOCKED, please UNLOCK/RETURN to edit!");
            return false;
        }
        currNotesTR = $(this).closest("tr");
        var MachineAreaID = currNotesTR.data("areaid");
        var MachineID = currNotesTR.data("machineid");
        var MachineYear = currNotesTR.data("year");
        var MachineMonth = currNotesTR.data("month");
        var currNotes = $(this).text();

        $("#modalMonthlyNotes #hfMachineAreaID").val(MachineAreaID);
        $("#modalMonthlyNotes #hfMachineID").val(MachineID);
        $("#modalMonthlyNotes #hfMachineYear").val(MachineYear);
        $("#modalMonthlyNotes #hfMachineMonth").val(MachineMonth);
        $("#modalMonthlyNotes #txtMachineMonthlyNotes").val(currNotes);
        $("#modalMonthlyNotes").modal("show");
    });

    $(".btnMonthlyNote").click(function () {
        if ($("#hfIsLock").val() == "Locked") {
            alert("This schedule is LOCKED, please UNLOCK/RETURN to edit!");
            return false;
        }
        var areaID = $("#modalMonthlyNotes #hfMachineAreaID").val();
        var machineID = $("#modalMonthlyNotes #hfMachineID").val();
        var currYear = $("#modalMonthlyNotes #hfMachineYear").val();
        var currMonth = $("#modalMonthlyNotes #hfMachineMonth").val();
        var currNotes = $("#modalMonthlyNotes #txtMachineMonthlyNotes").val();

        $("#modalMonthlyNotes").LoadingOverlay("show");
        $.ajax({
            url: "/NGKBusi/MTN/MachineDatabase/setMaintenanceScheduleMonthlyNote",
            method: "POST",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iAreaID: areaID,
                iMachineID: machineID,
                iYear: currYear,
                iMonth: currMonth,
                iNotes: currNotes
            }, success: function (data) {
                currNotesTR.find(".tdScheduleMonthDataNote").text(currNotes);
                $("#modalMonthlyNotes").LoadingOverlay("hide");
                $("#modalMonthlyNotes").modal("hide");
            }, error: function () {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                currTD.LoadingOverlay("hide");
                $("#modalMonthlyNotes").modal("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
});