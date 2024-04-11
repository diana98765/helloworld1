import pygame
import sys
import random
import copy

WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
DARK_VIOLET = (148, 0, 211)
GREEN_BLUE = (0, 153, 153)
CORAL = (255, 127, 80)
LIGHT_GRAY = (192, 192, 192)
FIREBRICK = (205, 38, 38)
ROYAL_BLUE = (72, 118, 255)
DODGER_BLUE4 = (16, 78, 139)
LightSkyBlue = (135, 206, 250)

block_size = 40
left_margin = 9 * block_size
upper_margin = 8 * block_size
size = (left_margin + 35 * block_size, upper_margin + 16 * block_size)
LETTERS = "АБВГДЕЖИКЛ"

pygame.init()

screen = pygame.display.set_mode(size)
pygame.display.set_caption("Морской бой")
font_size = int(block_size / 1.5)
font = pygame.font.SysFont('Cambria', font_size)
game_over_font_size = 3 * block_size
game_over_font = pygame.font.SysFont('Cambria', game_over_font_size)

# --Компьютер----------------------------------------------------------------------------------------------------------
computer_available_to_fire_set = {(x, y)
                                  for x in range(16, 26) for y in range(1, 11)}
around_last_computer_hit_set = set()
dotted_set_for_computer_not_to_shoot = set()
hit_blocks_for_computer_not_to_shoot = set()
last_hits_list = []
# ---------------------------------------------------------------------------------------------------------------------

hit_blocks = set()
dotted_set = set()
destroyed_computer_ships = []

# ---Поле--------------------------------------------------------------------------------------------------------------
class Grid:
    def __init__(self, title, offset):  # инициализация
        self.title = title
        self.offset = offset
        self.__draw_grid()
        self.__add_nums_letters_to_grid()
        self.__sign_grid()

    def __draw_grid(self):  # отрисовка игрового поля
        # левое поле
        pygame.draw.rect(screen, LightSkyBlue, ((left_margin + self.offset * 9 * block_size, 8 * block_size),
                                                (10 * block_size, 10 * block_size)))
        # правное поле
        pygame.draw.rect(screen, LightSkyBlue, ((2.67 * left_margin +  self.offset * 9 * block_size, 8 * block_size),
                                                (10 * block_size, 10 * block_size)))
        for i in range(11):
            # Горизонтальные линии
            pygame.draw.line(screen, ROYAL_BLUE, (left_margin + self.offset * block_size, upper_margin + i * block_size),
                             (left_margin + (11 + self.offset) * block_size, upper_margin + i * block_size), 2)
            # Вертикальные линии
            pygame.draw.line(screen, ROYAL_BLUE, (left_margin + (i + self.offset) * block_size, upper_margin),
                             (left_margin + (i + self.offset) * block_size, upper_margin + 11 * block_size), 2)

    def __add_nums_letters_to_grid(self):  # отрисовка букв и номеров клеток
        for i in range(10):
            num_ver = font.render(str(i + 1), True, DARK_VIOLET)
            letters_hor = font.render(LETTERS[i], True, DARK_VIOLET)
            num_ver_width = num_ver.get_width()
            num_ver_height = num_ver.get_height()
            letters_hor_width = letters_hor.get_width()

            screen.blit(num_ver, (left_margin * 2.23 - (block_size // 2 + num_ver_width // 2) + self.offset * block_size,
                                  upper_margin + i * block_size + (block_size // 2 - num_ver_height // 2)))
            screen.blit(letters_hor, (left_margin + i * block_size + (block_size // 2 - letters_hor_width // 2) +
                                      self.offset * block_size, upper_margin + 10 * block_size))

    def __sign_grid(self):  # подписи игровых полей
        player = font.render(self.title, True, CORAL)
        sign_width = player.get_width()
        screen.blit(player, (left_margin + 5 * block_size - sign_width // 2 +
                             self.offset * block_size, upper_margin - block_size // 2 - font_size))


class Button:
    def __init__(self, x_offset, button_title, message_to_show):  # инициализация
        self.__title = button_title
        self.__title_width, self.__title_height = font.size(self.__title)
        self.__message = message_to_show
        self.__button_width = self.__title_width + block_size
        self.__button_height = self.__title_height + block_size
        self.__x_start = x_offset
        self.__y_start = upper_margin + -7.5 * block_size + self.__button_height
        self.rect_for_draw = self.__x_start, self.__y_start, self.__button_width, self.__button_height
        self.rect = pygame.Rect(self.rect_for_draw)
        self.__rect_for_button_title = self.__x_start + self.__button_width / 2 - \
            self.__title_width / 2, self.__y_start + \
            self.__button_height / 2 - self.__title_height / 2
        self.__color = BLACK

    def draw_button(self, color=DODGER_BLUE4):  # кнопка
         if not color:
            color = self.__color
         pygame.draw.rect(screen, color, self.rect_for_draw)
         text_to_blit = font.render(self.__title, True, WHITE)
         screen.blit(text_to_blit, self.__rect_for_button_title)

    def change_color_on_hover(self):  # изменить цвет кнопки при наведении на неё мышкой
        mouse = pygame.mouse.get_pos()
        if self.rect.collidepoint(mouse):
            self.draw_button(GREEN_BLUE)

    def print_message_for_button(self):  # текст вопроса к кнопке
        message_width, message_height = font.size(self.__message)
        rect_for_message = self.__x_start / 2 - message_width / \
            2, self.__y_start + self.__button_height / 2 - message_height / 2
        text = font.render(self.__message, True, DARK_VIOLET)
        screen.blit(text, rect_for_message)


class AutoShips:
    def __init__(self, offset):  # нициализация
        self.offset = offset
        self.available_blocks = {(x, y) for x in range(
            1 + self.offset, 11 + self.offset) for y in range(1, 11)}
        self.ships_set = set()
        self.ships = self.__populate_grid()
        self.orientation = None
        self.direction = None

    def __create_start_block(self, available_blocks):
        # Случайным образом выбирает: 1. блок, с которого начать создание корабля
        #                             2. горизонтальный или вертикальный тип корабля
        #                             3. направление (из стартового блока) - прямое или обратное
        self.orientation = random.randint(0, 1)
        self.direction = random.choice((-1, 1))
        x, y = random.choice(tuple(available_blocks))
        return x, y, self.orientation, self.direction

    def __create_ship(self, number_of_blocks, available_blocks):
        # Создает корабль заданной длины, начиная с его стартового блока, используя функцию __create_start_block
        # изменяет корабль также этой функцией, если он выходит за пределы сетки. Проверка не прикасается ли он к др.
        # кораблям. После добавляет в список кораблей. По итогу функция возвращает список с координатам нового корабля
        ship_coordinates = []
        x, y, self.orientation, self.direction = self.__create_start_block(
            available_blocks)
        for _ in range(number_of_blocks):
            ship_coordinates.append((x, y))
            if not self.orientation:
                self.direction, x = self.__get_new_block_for_ship(
                    x, self.direction, self.orientation, ship_coordinates)
            else:
                self.direction, y = self.__get_new_block_for_ship(
                    y, self.direction, self.orientation, ship_coordinates)
        if self.__is_ship_valid(ship_coordinates):
            return ship_coordinates
        return self.__create_ship(number_of_blocks, available_blocks)

    def __get_new_block_for_ship(self, coor, direction, orientation, ship_coordinates):
        # Проверяет есть ли новые отдельные блоки, которые добавляются к кораблю в __create_ship
        # в пределах сетки, если нет, то в противном случае изменяет направление.
        self.direction = direction
        self.orientation = orientation
        if (coor <= 1 - self.offset * (self.orientation - 1) and self.direction == -1) or (
                coor >= 10 - self.offset * (self.orientation - 1) and self.direction == 1):
            self.direction *= -1
            return self.direction, ship_coordinates[0][self.orientation] + self.direction
        else:
            return self.direction, ship_coordinates[-1][self.orientation] + self.direction

    def __is_ship_valid(self, new_ship):  # Проверяет все ли координаты корабля находятся в пределах набора доступных
        ship = set(new_ship)              # блоков
        return ship.issubset(self.available_blocks)

    def __add_new_ship_to_set(self, new_ship):  # Корабли добавляются в ships_set
        self.ships_set.update(new_ship)

    def __update_available_blocks_for_creating_ships(self, new_ship):  # Обновляет блоки доступные для создания
        for elem in new_ship:                                          # кораблей. Удаляет занятые др. кораблями
            for k in range(-1, 2):                                     # и вокуг него из доступных блоков
                for m in range(-1, 2):
                    if self.offset < (elem[0] + k) < 11 + self.offset and 0 < (elem[1] + m) < 11:
                        self.available_blocks.discard(
                            (elem[0] + k, elem[1] + m))

    def __populate_grid(self):  # Создание определённого кол-ва кораблей каждого типа, вызывая функцию create_ship
        ships_coordinates_list = []
        for number_of_blocks in range(4, 0, -1):
            for _ in range(5 - number_of_blocks):
                new_ship = self.__create_ship(
                    number_of_blocks, self.available_blocks)
                ships_coordinates_list.append(new_ship)
                self.__add_new_ship_to_set(new_ship)
                self.__update_available_blocks_for_creating_ships(new_ship)
        return ships_coordinates_list


# --Стрельба-----------------------------------------------------------------------------------------------------------
def computer_shoots(set_to_shoot_from):  # Компьютер случайно выбирает блок, куда выстрелить, из доступных для
    pygame.time.delay(500)               # выстрелов блоков
    computer_fired_block = random.choice(tuple(set_to_shoot_from))
    computer_available_to_fire_set.discard(computer_fired_block)
    return computer_fired_block


def check_hit_or_miss(fired_block, opponents_ships_list, computer_turn, opponents_ships_list_original_copy,
                      opponents_ships_set):
    # Проверка блока, по которому был произведен выстрел. Это был компьютер или человек? это было "попадание" или "мимо"
    # Обновление. Точки - в пропущенных блоках или в диагональных блоках вокруг блока попадания.
    # Крестики - в блоках попадания. Потом удаляет убитые корабли из списка кораблей.
    for elem in opponents_ships_list:
        diagonal_only = True
        if fired_block in elem:
            ind = opponents_ships_list.index(elem)
            if len(elem) == 1:
                diagonal_only = False
            update_dotted_and_hit_sets(
                fired_block, computer_turn, diagonal_only)
            elem.remove(fired_block)
            opponents_ships_set.discard(fired_block)
            if computer_turn:
                last_hits_list.append(fired_block)
                update_around_last_computer_hit(fired_block, True)
            if not elem:
                update_destroyed_ships(
                    ind, computer_turn, opponents_ships_list_original_copy)
                if computer_turn:
                    last_hits_list.clear()
                    around_last_computer_hit_set.clear()
                else:
                    destroyed_computer_ships.append(computer.ships[ind])
            return True
    add_missed_block_to_dotted_set(fired_block)
    if computer_turn:
        update_around_last_computer_hit(fired_block, False)
    return False


def update_destroyed_ships(ind, computer_turn, opponents_ships_list_original_copy):  # обновление убитых кораблей
    ship = sorted(opponents_ships_list_original_copy[ind])
    for i in range(-1, 1):
        update_dotted_and_hit_sets(ship[i], computer_turn, False)


def update_around_last_computer_hit(fired_block, computer_hits):
    # Если Компьютер попал в корабль Пользователя, то Компьютер будет искать оставшиеся блоки корабля рядом
    global around_last_computer_hit_set, computer_available_to_fire_set
    if computer_hits and fired_block in around_last_computer_hit_set:
        around_last_computer_hit_set = computer_hits_twice()
    elif computer_hits and fired_block not in around_last_computer_hit_set:
        computer_first_hit(fired_block)
    elif not computer_hits:
        around_last_computer_hit_set.discard(fired_block)

    around_last_computer_hit_set -= dotted_set_for_computer_not_to_shoot
    around_last_computer_hit_set -= hit_blocks_for_computer_not_to_shoot
    computer_available_to_fire_set -= around_last_computer_hit_set
    computer_available_to_fire_set -= dotted_set_for_computer_not_to_shoot


def computer_first_hit(fired_block):
    # Добавляет блоки вокруг поражённого (не диагональные) в список, где предположительно может находиться корабль
    x_hit, y_hit = fired_block
    if x_hit > 16:
        around_last_computer_hit_set.add((x_hit - 1, y_hit))
    if x_hit < 25:
        around_last_computer_hit_set.add((x_hit + 1, y_hit))
    if y_hit > 1:
        around_last_computer_hit_set.add((x_hit, y_hit - 1))
    if y_hit < 10:
        around_last_computer_hit_set.add((x_hit, y_hit + 1))


def computer_hits_twice():
    # Добавляет блоки до и после двух или более блоков корабля во временный список, чтобы Компьютер быстрее убил корабль
    last_hits_list.sort()
    new_around_last_hit_set = set()
    for i in range(len(last_hits_list) - 1):
        x1 = last_hits_list[i][0]
        x2 = last_hits_list[i + 1][0]
        y1 = last_hits_list[i][1]
        y2 = last_hits_list[i + 1][1]
        if x1 == x2:
            if y1 > 1:
                new_around_last_hit_set.add((x1, y1 - 1))
            if y2 < 10:
                new_around_last_hit_set.add((x1, y2 + 1))
        elif y1 == y2:
            if x1 > 16:
                new_around_last_hit_set.add((x1 - 1, y1))
            if x2 < 25:
                new_around_last_hit_set.add((x2 + 1, y1))
    return new_around_last_hit_set


def update_dotted_and_hit_sets(fired_block, computer_turn, diagonal_only=True):
    # Расстановка точек
    global dotted_set
    x, y = fired_block
    a = 15 * computer_turn
    b = 11 + 15 * computer_turn
    hit_blocks_for_computer_not_to_shoot.add(fired_block)
    hit_blocks.add(fired_block)
    for i in range(-1, 2):
        for j in range(-1, 2):
            if (not diagonal_only or i != 0 and j != 0) and a < x + i < b and 0 < y + j < 11:
                add_missed_block_to_dotted_set((x + i, y + j))
    dotted_set -= hit_blocks


def add_missed_block_to_dotted_set(fired_block):
    # Добавляет fired_block к выстрелам промахам (если fired_block - это промах), чтобы потом нарисовать на них точки.
    # Убирает эти блоки из доступных для выстрела
    dotted_set.add(fired_block)
    dotted_set_for_computer_not_to_shoot.add(fired_block)


# --Обрисовка----------------------------------------------------------------------------------------------------------

def draw_ships(ships_coordinates_list):  # Обрисовать блоки, которые являются кораблями
    for elem in ships_coordinates_list:
        ship = sorted(elem)
        x_start = ship[0][0]
        y_start = ship[0][1]

        ship_width = block_size * len(ship)
        ship_height = block_size

        if len(ship) > 1 and ship[0][0] == ship[1][0]:
            ship_width, ship_height = ship_height, ship_width
        x = block_size * (x_start - 1) + left_margin
        y = block_size * (y_start - 1) + upper_margin
        pygame.draw.rect(
            screen, CORAL, ((x, y), (ship_width, ship_height)), width=block_size // 10)


def draw_from_dotted_set(dotted_set_to_draw_from):  # Рисует точку в середине блока
    for elem in dotted_set_to_draw_from:
        pygame.draw.circle(screen, BLACK, (block_size * (
            elem[0] - 0.5) + left_margin, block_size * (elem[1] - 0.5) + upper_margin), block_size // 6)


def draw_hit_blocks(hit_blocks_to_draw_from):  # Нарисовать крестик в поражённый блок корабля
    for block in hit_blocks_to_draw_from:
        x1 = block_size * (block[0] - 1) + left_margin
        y1 = block_size * (block[1] - 1) + upper_margin
        pygame.draw.line(screen, CORAL, (x1, y1),
                         (x1 + block_size, y1 + block_size), block_size // 6)
        pygame.draw.line(screen, CORAL, (x1, y1 + block_size),
                         (x1 + block_size, y1), block_size // 6)


def show_message_at_rect_center(message, rect, which_font=font, color=FIREBRICK):
    # Сообщение в центре экрана (вывод о победе Компьютера или Пользователя)
    message_width, message_height = which_font.size(message)
    message_rect = pygame.Rect(rect)
    x_start = message_rect.centerx - message_width / 2
    y_start = message_rect.centery - message_height / 2
    background_rect = pygame.Rect(
        x_start - block_size / 2, y_start, message_width + block_size, message_height)
    message_to_blit = which_font.render(message, True, color)
    screen.fill(WHITE, background_rect)
    screen.blit(message_to_blit, (x_start, y_start))


def ship_is_valid(ship_set, blocks_for_manual_drawing):  # Поверяет не соприкосается ли корабль с другими кораблями
    return ship_set.isdisjoint(blocks_for_manual_drawing)


def check_ships_numbers(ship, num_ships_list):  # Проверяет не превышает ли корабль определённой длины
    return (5 - len(ship)) > num_ships_list[len(ship)-1]


def update_used_blocks(ship, method):  # Обновление использованных блоков
    for block in ship:
        for i in range(-1, 2):
            for j in range(-1, 2):
                method((block[0]+i, block[1]+j))


# Создание кораблей Компьютера
computer = AutoShips(0)
computer_ships_working = copy.deepcopy(computer.ships)

# Создание кнопок "Автоматически", "Вручную" и текста пояснения к ним
auto_button_place = left_margin + 17 * block_size
manual_button_place = left_margin + 24 * block_size
how_to_create_ships_message = "Хотите автоматически создать корабли или вручную?"
auto_button = Button(auto_button_place, "Автоматически", how_to_create_ships_message)
manual_button = Button(manual_button_place, "Вручную", how_to_create_ships_message)

# Создание кнопки "Стереть последний корабль" и текста пояснения к ней при выборе создания кораблей вручную
undo_message = "Убрать последний созданный корабль?"
undo_button_place = left_margin + 12 * block_size
undo_button = Button(undo_button_place, "Стереть последний корабль", undo_message)

# Создать кнопку выхода и пояснения к ней
play_again_message = "Хотите выйти?"
play_again_button = Button(
    left_margin + 15 * block_size, "", play_again_message)
quit_game_button = Button(manual_button_place, "Выйти", play_again_message)



def main():
    ships_creation_not_decided = True
    ships_not_created = True
    drawing = False
    game_over = False
    computer_turn = False
    start = (0, 0)
    ship_size = (0, 0)

    rect_for_grids = (0, 0, size[0], upper_margin + 12 * block_size)
    rect_for_messages_and_buttons = (
        0, upper_margin + -7.5 * block_size, size[0], 5 * block_size)
    message_rect_for_drawing_ships = (undo_button.rect_for_draw[0] + undo_button.rect_for_draw[2], upper_margin +
                                      11 * block_size, size[0]-(undo_button.rect_for_draw[0] +
                                                                undo_button.rect_for_draw[2]), 4 * block_size)
    message_rect_computer = (left_margin - 2 * block_size, upper_margin +
                             11 * block_size, 14 * block_size, 4 * block_size)
    message_rect_human = (left_margin + 15 * block_size, upper_margin +
                          11 * block_size, 10 * block_size, 4 * block_size)

    human_ships_to_draw = []
    human_ships_set = set()
    used_blocks_for_manual_drawing = set()
    num_ships_list = [0, 0, 0, 0]

    screen.fill(WHITE)
    computer_grid = Grid("ИИ (компьютер)", 0)
    human_grid = Grid("Вы (человек)", 15)

    while ships_creation_not_decided:
        auto_button.draw_button()
        manual_button.draw_button()
        auto_button.change_color_on_hover()
        manual_button.change_color_on_hover()
        auto_button.print_message_for_button()

        mouse = pygame.mouse.get_pos()
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                game_over = True
                ships_creation_not_decided = False
                ships_not_created = False
            elif event.type == pygame.MOUSEBUTTONDOWN and auto_button.rect.collidepoint(mouse):
                human = AutoShips(15)
                human_ships_to_draw = human.ships
                human_ships_working = copy.deepcopy(human.ships)
                human_ships_set = human.ships_set
                ships_creation_not_decided = False
                ships_not_created = False
            elif event.type == pygame.MOUSEBUTTONDOWN and manual_button.rect.collidepoint(mouse):
                ships_creation_not_decided = False

        pygame.display.update()
        screen.fill(WHITE, rect_for_messages_and_buttons)  # <-Перекрытие ненужных кнопок------------------------------

    while ships_not_created:
        screen.fill(WHITE, rect_for_grids)
        computer_grid = Grid("ИИ (компьютер)", 0)
        human_grid = Grid("Вы (человек)", 15)
        undo_button.draw_button()
        undo_button.print_message_for_button()
        undo_button.change_color_on_hover()
        mouse = pygame.mouse.get_pos()
        if not human_ships_to_draw:
            undo_button.draw_button(LIGHT_GRAY)
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                ships_not_created = False
                game_over = True
            elif undo_button.rect.collidepoint(mouse) and event.type == pygame.MOUSEBUTTONDOWN:
                if human_ships_to_draw:
                    screen.fill(WHITE, message_rect_for_drawing_ships)
                    deleted_ship = human_ships_to_draw.pop()
                    num_ships_list[len(deleted_ship) - 1] -= 1
                    update_used_blocks(
                        deleted_ship, used_blocks_for_manual_drawing.discard)
            elif event.type == pygame.MOUSEBUTTONDOWN:
                drawing = True
                x_start, y_start = event.pos
                start = x_start, y_start
                ship_size = (0, 0)
            elif drawing and event.type == pygame.MOUSEMOTION:
                x_end, y_end = event.pos
                end = x_end, y_end
                ship_size = x_end - x_start, y_end - y_start
            elif drawing and event.type == pygame.MOUSEBUTTONUP:
                x_end, y_end = event.pos
                drawing = False
                ship_size = (0, 0)
                start_block = ((x_start - left_margin) // block_size + 1,
                               (y_start - upper_margin) // block_size + 1)
                end_block = ((x_end - left_margin) // block_size + 1,
                             (y_end - upper_margin) // block_size + 1)
                if start_block > end_block:
                    start_block, end_block = end_block, start_block
                temp_ship = []
                if 15 < start_block[0] < 26 and 0 < start_block[1] < 11 and 15 < end_block[0] < 26 and\
                        0 < end_block[1] < 11:
                    screen.fill(WHITE, message_rect_for_drawing_ships)
                    if start_block[0] == end_block[0] and (end_block[1] - start_block[1]) < 4:
                        for block in range(start_block[1], end_block[1]+1):
                            temp_ship.append((start_block[0], block))
                    elif start_block[1] == end_block[1] and (end_block[0] - start_block[0]) < 4:
                        for block in range(start_block[0], end_block[0]+1):
                            temp_ship.append((block, start_block[1]))
                    else:
                        show_message_at_rect_center(
                            "Корабль слишком велик", message_rect_for_drawing_ships)
                else:
                    show_message_at_rect_center(
                        "Выход за предел сетки", message_rect_for_drawing_ships)
                if temp_ship:
                    temp_ship_set = set(temp_ship)
                    if ship_is_valid(temp_ship_set, used_blocks_for_manual_drawing):
                        if check_ships_numbers(temp_ship, num_ships_list):
                            num_ships_list[len(temp_ship) - 1] += 1
                            human_ships_to_draw.append(temp_ship)
                            human_ships_set |= temp_ship_set
                            update_used_blocks(
                                temp_ship, used_blocks_for_manual_drawing.add)
                        else:
                            if (len(temp_ship) == 1):
                                show_message_at_rect_center(
                                    f"Уже хватает однопалубных кораблей", message_rect_for_drawing_ships)
                            elif (len(temp_ship) == 2):
                                show_message_at_rect_center(
                                    f"Уже хватает двухпалубных кораблей", message_rect_for_drawing_ships)
                            elif (len(temp_ship) == 3):
                                show_message_at_rect_center(
                                    f"Уже хватает трёхпалубных кораблей", message_rect_for_drawing_ships)
                            elif (len(temp_ship) == 4):
                                show_message_at_rect_center(
                                    f"Уже хватает четырёхпалубных кораблей", message_rect_for_drawing_ships)
                    else:
                        show_message_at_rect_center(
                            "Корабли соприкосаются", message_rect_for_drawing_ships)
            if len(human_ships_to_draw) == 10:
                ships_not_created = False
                human_ships_working = copy.deepcopy(human_ships_to_draw)
                screen.fill(WHITE, rect_for_messages_and_buttons)
        pygame.draw.rect(screen, BLACK, (start, ship_size), 3)
        draw_ships(human_ships_to_draw)
        pygame.display.update()

    while not game_over:
        draw_ships(destroyed_computer_ships)
        draw_ships(human_ships_to_draw)
        if not (dotted_set | hit_blocks):
            show_message_at_rect_center(
                "Игра началась! Делайте ход", message_rect_computer)
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                game_over = True
            elif not computer_turn and event.type == pygame.MOUSEBUTTONDOWN:
                x, y = event.pos
                if (left_margin < x < left_margin + 10 * block_size) and (
                        upper_margin < y < upper_margin + 10 * block_size):
                    fired_block = ((x - left_margin) // block_size + 1,
                                   (y - upper_margin) // block_size + 1)
                    computer_turn = not check_hit_or_miss(fired_block, computer_ships_working, False, computer.ships,
                                                          computer.ships_set)
                    draw_from_dotted_set(dotted_set)
                    draw_hit_blocks(hit_blocks)
                    screen.fill(WHITE, message_rect_computer)
                    show_message_at_rect_center(
                        f"Ваш последний выстрел: {LETTERS[fired_block[0]-1] + str(fired_block[1])}", message_rect_computer, color=DARK_VIOLET)
                else:
                    show_message_at_rect_center(
                        "Вы пытаетесь стрельнуть мимо сетки", message_rect_computer)
        if computer_turn:
            set_to_shoot_from = computer_available_to_fire_set
            if around_last_computer_hit_set:
                set_to_shoot_from = around_last_computer_hit_set
            fired_block = computer_shoots(set_to_shoot_from)
            computer_turn = check_hit_or_miss(
                fired_block, human_ships_working, True, human_ships_to_draw, human_ships_set)
            draw_from_dotted_set(dotted_set)
            draw_hit_blocks(hit_blocks)
            screen.fill(WHITE, message_rect_human)
            show_message_at_rect_center(
                f"Последний выстрел ИИ: {LETTERS[fired_block[0] - 16] + str(fired_block[1])}", message_rect_human, color=DARK_VIOLET)
        if not computer.ships_set:
            show_message_at_rect_center(
                "Вы победили компьютера!", (0, 0, size[0], size[1]), game_over_font)
            game_over = True
        if not human_ships_set:
            show_message_at_rect_center(
                "Компьютер победил вас!", (0, 0, size[0], size[1]), game_over_font)
            game_over = True
        pygame.display.update()

    while game_over:
        screen.fill(WHITE, rect_for_messages_and_buttons)
        play_again_button.print_message_for_button()
        quit_game_button.draw_button()
        quit_game_button.change_color_on_hover()

        mouse = pygame.mouse.get_pos()
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                pygame.quit()
                sys.exit()
            elif event.type == pygame.MOUSEBUTTONDOWN and quit_game_button.rect.collidepoint(mouse):
                pygame.quit()
                sys.exit()
        pygame.display.update()


main()
