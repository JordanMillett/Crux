from gimpfu import *

def texture_process(image, drawable):

    width = pdb.gimp_image_width(image)
    height = pdb.gimp_image_height(image)
    new_size = max(width, height)

    pdb.gimp_image_resize(image, new_size, new_size, (new_size - width) // 2, (new_size - height) // 2)

    pdb.gimp_context_set_interpolation(INTERPOLATION_CUBIC)
    pdb.gimp_image_scale(image, 128, 128)

    layer = pdb.gimp_image_get_active_layer(image)
    pdb.plug_in_median_blur(image, layer, 1, 50)
    pdb.plug_in_gauss_iir(image, layer, 1.5, True, True)
    pdb.gimp_drawable_hue_saturation(layer, 0, 0, 0, 25, 0)
    
    offset_x = (layer.width - pdb.gimp_image_width(image)) // 2
    offset_y = (layer.height - pdb.gimp_image_height(image)) // 2
    pdb.gimp_image_resize(image, layer.width, layer.height, offset_x, offset_y)

    pdb.gimp_displays_flush()

register(
    "blurry_texture_process",
    "Process a seamless texture with scaling, noise, and indexed color conversion",
    "Automates the texture processing workflow",
    "Your Name",
    "Your License",
    "2025",
    "<Image>/Filters/Custom/Blurry Texture Process",
    "RGB*, GRAY*",
    [],
    [],
    texture_process
)

main()
