/* BCP Dummy Interactions */
$(document).ready(function () {
    // Handle Score Button Selection
    $(document).on('click', '.bcp-score-btn', function () {
        const row = $(this).closest('tr');
        row.find('.bcp-score-btn').removeClass('selected btn-primary').addClass('btn-outline-secondary');
        $(this).addClass('selected btn-primary').removeClass('btn-outline-secondary');
        
        // Dummy calculation update
        updateDummyTotalScore();
    });

    // Mock tooltips
    $('[data-toggle="tooltip"]').tooltip();

    // DataTables initialization
    if ($('.bcp-datatable').length) {
        $('.bcp-datatable').DataTable({
            responsive: true,
            pageLength: 10,
            dom: '<"top"f>rt<"bottom"lp><"clear">',
            language: {
                search: "_INPUT_",
                searchPlaceholder: "Search incidents..."
            }
        });
    }
});

function updateDummyTotalScore() {
    let total = 0;
    let count = 0;
    $('.bcp-score-btn.selected').each(function() {
        total += parseInt($(this).text());
        count++;
    });
    
    if (count > 0) {
        const avg = (total / count).toFixed(2);
        $('#dummy-total-score').text(avg + ' / 5.00');
        
        // Update color based on score
        const display = $('#dummy-score-display');
        display.removeClass('text-success text-warning text-danger');
        if (avg >= 4) display.addClass('text-danger');
        else if (avg >= 2.5) display.addClass('text-warning');
        else display.addClass('text-success');
    }
}

// BCP Activation Simulation
function simulateActivation() {
    Swal.fire({
        title: 'Activate BCP?',
        text: "This will trigger emergency protocols and notify all committee members!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc2626',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, ACTIVATE',
        showLoaderOnConfirm: true,
        preConfirm: () => {
            return new Promise((resolve) => {
                setTimeout(() => {
                    resolve();
                }, 2000);
            });
        }
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire(
                'BCP ACTIVATED!',
                'Notifications have been sent to all stakeholders.',
                'success'
            );
        }
    });
}

// Make Demo Control Panel Draggable
$(document).ready(function() {
    // Find the Demo Control Panel container
    let $demoPanel = $('.position-fixed, .fixed').filter(function() {
        let text = $(this).text();
        return (text.indexOf('Demo Control Panel') !== -1) && 
               ($(this).css('z-index') == '1050' || $(this).hasClass('z-[1050]'));
    });

    if ($demoPanel.length) {
        let isDragging = false;
        let currentX;
        let currentY;
        let initialX;
        let initialY;
        let xOffset = 0;
        let yOffset = 0;

        // Target the header to be the drag handler
        let $header = $demoPanel.find('.card-header, .bg-indigo-900').first();
        if ($header.length === 0) $header = $demoPanel; // fallback
        
        $header.css({
            'cursor': 'move',
            'user-select': 'none'
        });

        $header.on('mousedown', function(e) {
            initialX = e.clientX - xOffset;
            initialY = e.clientY - yOffset;
            isDragging = true;
        });

        $(document).on('mouseup', function() {
            initialX = currentX;
            initialY = currentY;
            isDragging = false;
        });

        $(document).on('mousemove', function(e) {
            if (isDragging) {
                e.preventDefault();
                currentX = e.clientX - initialX;
                currentY = e.clientY - initialY;
                xOffset = currentX;
                yOffset = currentY;
                $demoPanel.css('transform', 'translate3d(' + currentX + 'px, ' + currentY + 'px, 0)');
            }
        });
    }
});
