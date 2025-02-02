﻿using recipe_book.Controls;
using System.Data.SQLite;

namespace recipe_book
{
    public sealed partial class MainForm : Form
    {
        private TimeSpan CookingTimeAsTimeSpan
        {
            get => new TimeSpan(
                7 * (int)(numWeeks.Value + numDays.Value),
                (int)numHours.Value,
                (int)numMinutes.Value,
                (int)numSeconds.Value,
                0,
                0
            );
        }

        private void btnCancelCreationOrEdition_Click(object sender, EventArgs e)
        {
            tbcMainFormTabs.SelectedTab = tabListOfRecipes;
            _previousSelectedTab = tabCreateOrEditRecipe;
        }

        private void btnLoadRecipePhoto_Click(object sender, EventArgs e)
        {
            if (dlgLoadRecipePhoto.ShowDialog() == DialogResult.OK)
                try
                {
                    picRecipePhoto.ImageLocation = dlgLoadRecipePhoto.FileName;
                    picRecipePhoto.Visible = true;
                    btnDeleteRecipePhoto.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        caption: "Ошибка добавления фотографии",
                        text: ex.Message,
                        buttons: MessageBoxButtons.OK,
                        icon: MessageBoxIcon.Error
                    );
                }
        }

        private void btnDeleteRecipePhoto_Click(object sender, EventArgs e)
        {
            picRecipePhoto.Visible = false;
            picRecipePhoto.Image = null;
            btnDeleteRecipePhoto.Enabled = false;
        }

        private void btnSaveRecipe_Click(object sender, EventArgs e)
        {
            SQLiteCommand cmd = DbModule.CreateCommand("""
                INSERT INTO Recipes (user_id, name, rating, cooking_time, photo, cooking_method, creation_time)
                VALUES ($user_id, $name, $rating, $cooking_time, $photo, $cooking_method, $creation_time)
                """,
                new SQLiteParameter("user_id", userId),
                new SQLiteParameter("name", txtRecipeName.Text),
                new SQLiteParameter("rating", numRecipeRating.Value),
                new SQLiteParameter("cooking_time", CookingTimeAsTimeSpan.Ticks),
                new SQLiteParameter("photo", picRecipePhoto.Image?.ToBytes()),
                new SQLiteParameter("cooking_method", txtCookingMethod.Text),
                new SQLiteParameter("creation_time", DateTime.Now.Ticks)
            );
            cmd.ExecuteNonQuery();

            SQLiteCommand cmdGetLastInsertedId = DbModule.CreateCommand("SELECT last_insert_rowid()");
            long recipeId = (long)cmdGetLastInsertedId.ExecuteScalar();
            long lastElementId;

            var autoFillingPanels = new[]
            {
                ("Tags", pnlTagInput),
                ("Ingredients", pnlIngredientInput)
            };
            foreach ((string tableName, AutoFillingFlowPanel panel) in autoFillingPanels)
                foreach (string value in panel.Values)
                {
                    cmd = DbModule.CreateCommand($"""
                        INSERT INTO {tableName} (name)
                        VALUES ($name)
                        """,
                        new SQLiteParameter("name", value)
                    );

					try
					{
						cmd.ExecuteNonQuery();
					}
					catch(Exception exception)
					{
						// Если ингридиент уже есть в списке,
						// может возникнуть исключение,
						// так как ингридиенты имеют уникальное
						// наименование
					}

                    lastElementId = (long)cmdGetLastInsertedId.ExecuteScalar();

                    cmd = DbModule.CreateCommand($"""
                        INSERT INTO Recipe{tableName}
                        VALUES ($recipe_id, $element_id)
                        """,
                        new SQLiteParameter("recipe_id", recipeId),
                        new SQLiteParameter("element_id", lastElementId)
                    );
                    cmd.ExecuteNonQuery();
                }
            _previousSelectedTab = tabCreateOrEditRecipe;
            tbcMainFormTabs.SelectedTab = tabListOfRecipes;
        }

        private void ClearRecipeInputFields()
        {
            txtRecipeName.Clear();
            txtCookingMethod.Clear();
            pnlTagInput.Clear();
            pnlIngredientInput.Clear();
            btnDeleteRecipePhoto_Click(new(), new());
            foreach (var numericUpDown in new[] { numHours, numMinutes, numSeconds, numWeeks, numDays, numRecipeRating })
                numericUpDown.Value = numericUpDown.Minimum;
        }

        private void RecipeInputFieldsChanged(object sender, EventArgs e)
        {
            btnSaveRecipe.Enabled = txtCookingMethod.TextLength > 0
                && txtRecipeName.TextLength > 0
                && pnlIngredientInput.Controls.Count > 0
                && CookingTimeAsTimeSpan > _minimumCookingTime;
        }
    }
}
