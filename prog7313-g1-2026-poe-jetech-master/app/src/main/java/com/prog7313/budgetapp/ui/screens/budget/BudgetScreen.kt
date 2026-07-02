package com.prog7313.budgetapp.ui.screens.budget

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.graphics.toColorInt
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.prog7313.budgetapp.data.model.CategoryWithSpending
import com.prog7313.budgetapp.ui.components.BudgetProgressBar
import com.prog7313.budgetapp.ui.components.SectionHeader
import com.prog7313.budgetapp.ui.theme.*
import com.prog7313.budgetapp.viewmodel.AppViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BudgetScreen(viewModel: AppViewModel) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val categoriesWithSpending = viewModel.categoriesWithSpending()

    var showBudgetDialog   by remember { mutableStateOf(false) }
    var showCategoryDialog by remember { mutableStateOf(false) }

    // Reload every time this screen appears so budget goal is always current
    LaunchedEffect(Unit) { viewModel.loadAll() }

    Scaffold(
        topBar = {
            TopAppBar(
                title  = { Text("Budget", fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { showCategoryDialog = true }) {
                        Icon(Icons.Default.Add, "Add Category")
                    }
                    IconButton(onClick = { showBudgetDialog = true }) {
                        Icon(Icons.Default.Edit, "Set Budget")
                    }
                }
            )
        }
    ) { padding ->
        LazyColumn(
            Modifier.fillMaxSize().padding(padding),
            contentPadding = PaddingValues(bottom = 16.dp)
        ) {
            // ── Monthly budget summary ────────────────────────────────────────
            item {
                val goal  = state.budgetGoal
                val spent = viewModel.totalSpent()
                Card(
                    modifier  = Modifier.fillMaxWidth().padding(16.dp),
                    shape     = RoundedCornerShape(16.dp),
                    elevation = CardDefaults.cardElevation(4.dp)
                ) {
                    Column(Modifier.padding(16.dp)) {
                        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                            Text("Expenses", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                            Text("Budgeted: R ${"%,.2f".format(goal?.totalBudget ?: 0.0)}", style = MaterialTheme.typography.bodySmall)
                        }
                        Spacer(Modifier.height(4.dp))
                        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                            Column {
                                Text("Spent", style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurface.copy(0.6f))
                                Text("R ${"%,.2f".format(spent)}", fontWeight = FontWeight.Bold)
                            }
                            Column(horizontalAlignment = Alignment.End) {
                                Text("Available", style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurface.copy(0.6f))
                                val avail = (goal?.totalBudget ?: 0.0) - spent
                                Text("R ${"%,.2f".format(avail)}", fontWeight = FontWeight.Bold,
                                    color = if (avail >= 0) AccentGreen else AccentRed)
                            }
                        }
                        if (goal != null && goal.totalBudget > 0) {
                            Spacer(Modifier.height(8.dp))
                            BudgetProgressBar(
                                progress       = (spent / goal.totalBudget).toFloat(),
                                budgetLabel    = "Spent last month: R ${"%,.0f".format(spent)}",
                                spentLabel     = "R ${"%,.0f".format(spent)}",
                                availableLabel = "R ${"%,.0f".format(goal.totalBudget - spent)}",
                                isOverBudget   = goal.maxAmount > 0 && spent > goal.maxAmount
                            )
                        }
                        if (goal == null) {
                            Spacer(Modifier.height(8.dp))
                            OutlinedButton(onClick = { showBudgetDialog = true }, modifier = Modifier.fillMaxWidth()) {
                                Icon(Icons.Default.Add, null, Modifier.size(16.dp))
                                Spacer(Modifier.width(4.dp))
                                Text("Set Monthly Budget")
                            }
                        }
                    }
                }
            }

            // ── Category rows ─────────────────────────────────────────────────
            item { SectionHeader("Expense Categories") }
            if (categoriesWithSpending.isEmpty()) {
                item {
                    Column(
                        Modifier.fillMaxWidth().padding(32.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text("📂", fontSize = 40.sp)
                        Spacer(Modifier.height(8.dp))
                        Text("No categories yet", style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurface.copy(0.5f))
                        Spacer(Modifier.height(8.dp))
                        Button(onClick = { showCategoryDialog = true }) { Text("Add Category") }
                    }
                }
            } else {
                items(categoriesWithSpending) { item ->
                    CategoryBudgetRow(item) { viewModel.deleteCategory(item.category.id) }
                }
            }
        }
    }

    // ── Dialogs ───────────────────────────────────────────────────────────────
    if (showBudgetDialog) {
        SetBudgetDialog(
            existing = state.budgetGoal,
            onDismiss = { showBudgetDialog = false },
            onSave    = { min, max, total ->
                viewModel.saveBudgetGoal(min, max, total)
                showBudgetDialog = false
            }
        )
    }
    if (showCategoryDialog) {
        AddCategoryDialog(
            onDismiss = { showCategoryDialog = false },
            onSave    = { name, icon, color, limit ->
                viewModel.createCategory(name, icon, color, limit)
                showCategoryDialog = false
            }
        )
    }
}

// ── Category row ──────────────────────────────────────────────────────────────

@Composable
private fun CategoryBudgetRow(item: CategoryWithSpending, onDelete: () -> Unit) {
    val catColor = try { Color(item.category.color.toColorInt()) } catch (e: Exception) { AccentBlue }
    Card(
        modifier  = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 4.dp),
        shape     = RoundedCornerShape(12.dp),
        elevation = CardDefaults.cardElevation(1.dp)
    ) {
        Row(Modifier.padding(12.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(
                Modifier.size(44.dp).clip(RoundedCornerShape(10.dp)).background(catColor.copy(0.15f)),
                contentAlignment = Alignment.Center
            ) { Text(item.category.icon, fontSize = 22.sp) }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                    Text(item.category.name, fontWeight = FontWeight.Medium)
                    Text("R ${"%,.2f".format(item.totalSpent)}", fontWeight = FontWeight.Bold,
                        color = if (item.isOverBudget) AccentRed else MaterialTheme.colorScheme.onSurface)
                }
                if (item.category.budgetLimit > 0) {
                    Spacer(Modifier.height(4.dp))
                    LinearProgressIndicator(
                        progress = { item.percentOfBudget },
                        modifier  = Modifier.fillMaxWidth().height(6.dp).clip(RoundedCornerShape(3.dp)),
                        color     = if (item.isOverBudget) AccentRed else catColor
                    )
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text("Limit: R ${"%,.0f".format(item.category.budgetLimit)}", style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurface.copy(0.5f))
                        if (item.isOverBudget)
                            Text("OVER BUDGET", style = MaterialTheme.typography.labelSmall, color = AccentRed, fontWeight = FontWeight.Bold)
                        else
                            Text("Left: R ${"%,.0f".format(item.category.budgetLimit - item.totalSpent)}", style = MaterialTheme.typography.labelSmall,
                                color = AccentGreen)
                    }
                }
            }
            IconButton(onClick = onDelete) { Icon(Icons.Default.Delete, null, tint = MaterialTheme.colorScheme.error) }
        }
    }
}

// ── Dialogs ───────────────────────────────────────────────────────────────────

@Composable
private fun SetBudgetDialog(
    existing: com.prog7313.budgetapp.data.model.BudgetGoal?,
    onDismiss: () -> Unit,
    onSave: (min: Double, max: Double, total: Double) -> Unit
) {
    var totalBudget  by remember { mutableStateOf(existing?.totalBudget?.toString() ?: "") }
    var minAmount    by remember { mutableStateOf(existing?.minAmount?.toString() ?: "") }
    var maxAmount    by remember { mutableStateOf(existing?.maxAmount?.toString() ?: "") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title   = { Text("Set Monthly Budget") },
        text    = {
            Column {
                OutlinedTextField(totalBudget, { totalBudget = it }, label = { Text("Total Budget (R)") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(minAmount, { minAmount = it }, label = { Text("Minimum Spending Goal (R)") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(maxAmount, { maxAmount = it }, label = { Text("Maximum Spending Limit (R)") }, singleLine = true, modifier = Modifier.fillMaxWidth())
            }
        },
        confirmButton = {
            TextButton(onClick = {
                onSave(
                    minAmount.toDoubleOrNull() ?: 0.0,
                    maxAmount.toDoubleOrNull() ?: 0.0,
                    totalBudget.toDoubleOrNull() ?: 0.0
                )
            }) { Text("Save") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } }
    )
}

@Composable
private fun AddCategoryDialog(
    onDismiss: () -> Unit,
    onSave: (name: String, icon: String, color: String, limit: Double) -> Unit
) {
    var name  by remember { mutableStateOf("") }
    var icon  by remember { mutableStateOf("💰") }
    var color by remember { mutableStateOf("#4A90D9") }
    var limit by remember { mutableStateOf("") }

    // Full set of category icons — two rows of 6
    val icons = listOf(
        "💰", "🛒", "🍔", "🚗", "🏠", "🎮",
        "✈️", "💊", "📚", "🎵", "👗", "⚡"
    )

    // Colour swatches the user can pick
    val colorOptions = listOf(
        "#4A90D9", "#4CAF50", "#FF9800", "#F44336",
        "#9C27B0", "#00BCD4", "#FF5722", "#607D8B"
    )

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("New Category", fontWeight = FontWeight.Bold) },
        text  = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {

                // ── Category name ─────────────────────────────────────────────
                OutlinedTextField(
                    value         = name,
                    onValueChange = { name = it },
                    label         = { Text("Category Name") },
                    singleLine    = true,
                    modifier      = Modifier.fillMaxWidth()
                )

                // ── Icon picker ───────────────────────────────────────────────
                Text("Choose Icon", style = MaterialTheme.typography.labelMedium)

                /*
                 * Two rows of 6 icons each.
                 * Each emoji lives in a 44×44 dp Box — large enough for proper
                 * rendering on all API levels. The selected icon gets a coloured
                 * background circle; unselected icons get a neutral surface.
                 */
                val rows = icons.chunked(6)
                rows.forEach { rowIcons ->
                    Row(
                        modifier            = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        rowIcons.forEach { i ->
                            val isSelected = icon == i
                            Box(
                                modifier = Modifier
                                    .size(44.dp)
                                    .clip(RoundedCornerShape(10.dp))
                                    .background(
                                        if (isSelected)
                                            try { Color(color.toColorInt()) }
                                            catch (e: Exception) { AccentBlue }
                                        else
                                            MaterialTheme.colorScheme.surfaceVariant
                                    )
                                    .then(
                                        Modifier.clickable { icon = i }
                                    ),
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text     = i,
                                    fontSize = 22.sp
                                )
                            }
                        }
                    }
                }

                // ── Colour picker ─────────────────────────────────────────────
                Text("Choose Colour", style = MaterialTheme.typography.labelMedium)
                Row(
                    modifier              = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    colorOptions.forEach { c ->
                        val isSelected = color == c
                        Box(
                            modifier = Modifier
                                .size(32.dp)
                                .clip(CircleShape)
                                .background(
                                    try { Color(c.toColorInt()) }
                                    catch (e: Exception) { AccentBlue }
                                )
                                .then(if (isSelected) Modifier.border(
                                    width = 3.dp,
                                    color = MaterialTheme.colorScheme.onSurface,
                                    shape = CircleShape
                                ) else Modifier)
                                .clickable { color = c }
                        )
                    }
                }

                // ── Budget limit ──────────────────────────────────────────────
                OutlinedTextField(
                    value         = limit,
                    onValueChange = { limit = it },
                    label         = { Text("Budget Limit (R, optional)") },
                    singleLine    = true,
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Decimal
                    ),
                    modifier      = Modifier.fillMaxWidth()
                )

                // ── Live preview ──────────────────────────────────────────────
                if (name.isNotBlank()) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        modifier          = Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(10.dp))
                            .background(MaterialTheme.colorScheme.surfaceVariant)
                            .padding(10.dp)
                    ) {
                        Box(
                            Modifier
                                .size(36.dp)
                                .clip(RoundedCornerShape(8.dp))
                                .background(
                                    try { Color(color.toColorInt()).copy(alpha = 0.2f) }
                                    catch (e: Exception) { AccentBlue.copy(0.2f) }
                                ),
                            contentAlignment = Alignment.Center
                        ) { Text(icon, fontSize = 18.sp) }
                        Spacer(Modifier.width(10.dp))
                        Text(name, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Medium)
                    }
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick  = { if (name.isNotBlank()) onSave(name, icon, color, limit.toDoubleOrNull() ?: 0.0) },
                enabled  = name.isNotBlank()
            ) { Text("Add") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } }
    )
}

/*
Title: Compose — Box with clickable modifier as a custom button
Author(s): Android Developers
Date: 2024
Version: Compose BOM 2024.06.00
Type: Documentation
Availability: https://developer.android.com/develop/ui/compose/gestures#clickable
*/

/*
Title: Kotlin — List.chunked() for splitting lists into fixed-size sublists
Author(s): JetBrains
Date: 2024
Version: Kotlin 2.0
Type: Documentation
Availability: https://kotlinlang.org/api/latest/jvm/stdlib/kotlin.collections/chunked.html
*/

/*
Title: Material 3 — AlertDialog composable
Author(s): Android Developers
Date: 2024
Version: Material3 1.2.1
Type: Documentation
Availability: https://developer.android.com/develop/ui/compose/components/dialog
*/

/*
Title: Material 3 — ExposedDropdownMenuBox for category selection
Author(s): Android Developers
Date: 2024
Version: Material3 1.2.1
Type: Documentation
Availability: https://developer.android.com/develop/ui/compose/components/dropdown
*/