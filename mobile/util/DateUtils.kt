package com.paravolley.mobile.util

import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeFormatterBuilder
import java.time.temporal.ChronoUnit
import java.util.Locale

object DateUtils {

    // Flexible formatter supporting formats like "15 May 2026", "2026-05-15", "15/05/2026"
    private val formatter = DateTimeFormatterBuilder()
        .parseCaseInsensitive()
        .appendPattern("[d MMMM yyyy][yyyy-MM-dd][dd/MM/yyyy][d MMM yyyy]")
        .toFormatter(Locale.ENGLISH)

    /**
     * Returns true if the given event date has already passed.
     */
    fun isEventPast(dateString: String): Boolean {
        return try {
            val eventDate = LocalDate.parse(dateString.trim(), formatter)
            eventDate.isBefore(LocalDate.now())
        } catch (e: Exception) {
            false
        }
    }

    /**
     * Returns human-friendly text comparing the event date to right now.
     * Examples: "Happening Today", "Tomorrow", "In 3 days", "Passed 2 days ago"
     */
    fun getRelativeTimeText(dateString: String): String {
        return try {
            val eventDate = LocalDate.parse(dateString.trim(), formatter)
            val today = LocalDate.now()
            val daysBetween = ChronoUnit.DAYS.between(today, eventDate)

            when {
                daysBetween < -1L -> "Passed ${-daysBetween} days ago"
                daysBetween == -1L -> "Passed Yesterday"
                daysBetween == 0L -> "Happening Today"
                daysBetween == 1L -> "Tomorrow"
                daysBetween in 2..7 -> "In $daysBetween days"
                daysBetween > 7 -> "In ${daysBetween / 7} weeks"
                else -> "Upcoming"
            }
        } catch (e: Exception) {
            dateString
        }
    }
}
